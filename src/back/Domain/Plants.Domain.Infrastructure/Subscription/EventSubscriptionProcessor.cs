﻿using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Plants.Domain.Infrastructure;

internal class EventSubscriptionProcessor
{
    private readonly RepositoriesCaller _caller;
    private readonly CqrsHelper _cqrs;
    private readonly IEventStore _eventStore;
    private readonly IServiceProvider _provider;
    private readonly ISubscriptionProcessingMarker _marker;
    private readonly IProjectionsUpdater _updater;
    private readonly INotificationSender _notificationSender;
    private readonly ISubscriptionProcessingSubscription _notificator;

    public EventSubscriptionProcessor(RepositoriesCaller caller, CqrsHelper cqrs,
        IEventStore eventStore, IServiceProvider provider,
        ISubscriptionProcessingMarker marker, IProjectionsUpdater updater,
        INotificationSender notificationSender, ISubscriptionProcessingSubscription notificator)
    {
        _caller = caller;
        _cqrs = cqrs;
        _eventStore = eventStore;
        _provider = provider;
        _marker = marker;
        _updater = updater;
        _notificationSender = notificationSender;
        _notificator = notificator;
    }

    public async Task<Exception?> ProcessCommandAsync(Command command, List<Event> aggEvents, CancellationToken token = default)
    {
        Exception? exception;
        try
        {
            var tasks = new[]
            {
                _updater.UpdateProjectionAsync(command.Metadata.Aggregate, token: token),
                UpdateSubscribersAsync(command, aggEvents, token)
            };
            await Task.WhenAll(tasks);
            exception = null;
        }
        catch (Exception e)
        {
            exception = e;
        }

        var finalAggregate = command.Metadata.InitialAggregate ?? command.Metadata.Aggregate;
        var subscription = _marker.MarkSubscriptionComplete(finalAggregate);
        if (subscription is not null && subscription.IsProcessed && subscription.NotifyUsername is not null && finalAggregate is not null)
        {
            await _notificationSender.SendNotificationAsync(subscription.NotifyUsername,
                                                            new(
                                                                new(
                                                                    command.Metadata.Id,
                                                                    command.Metadata.Name, 
                                                                    command.Metadata.Time,
                                                                    finalAggregate),
                                                                exception is null),
                                                            token);
            _notificator.UnsubscribeFromNotifications(finalAggregate);
        }

        return exception;
    }

    private async Task UpdateSubscribersAsync(Command parentCommand, List<Event> aggEvents, CancellationToken token = default)
    {
        if (_cqrs.EventSubscriptions.TryGetValue(parentCommand.Metadata.Aggregate.Name, out var subscriptions))
        {
            foreach (var subscription in subscriptions)
            {
                var eventsToHandle = subscription.Filter.Match(
                  filter =>
                  {
                      var eventNames = filter.EventNames.Select(x => x.Replace("Event", ""));
                      return aggEvents.Where(x => eventNames.Contains(x.Metadata.Name));
                  },
                  all => aggEvents);
                if (eventsToHandle.Any())
                {
                    var applyerType = GetApplyerTypeFor(subscription);
                    var applyer = _provider.GetRequiredService(applyerType);
                    var method = applyerType.GetMethod(nameof(TransposeApplyer<AggregateBase>.CallTransposeAsync), BindingFlags.Public | BindingFlags.Instance)!;
                    var transposedEvents = (IEnumerable<Event>)await (dynamic)method.Invoke(applyer, new[] { subscription.Transpose, eventsToHandle, token })!;
                    var firstEvent = transposedEvents.FirstOrDefault();
                    if (firstEvent != default)
                    {
                        var firstEventAggregate = firstEvent.Metadata.Aggregate;
                        var aggregate = await _caller.LoadAsync(firstEventAggregate, token: token);
                        var command = parentCommand.ChangeTargetAggregate(firstEventAggregate);
                        if (aggregate.Metadata.CommandsProcessedIds.Contains(command.Metadata.Id) is false)
                        {
                            _marker.MarkSubscribersCount(command.Metadata.InitialAggregate!, 1);
                            var commandNumber = await _eventStore.AppendCommandAsync(command, aggregate.Metadata.Version, token);
                            await _eventStore.AppendEventsAsync(transposedEvents, commandNumber, command, token);
                        }
                    }
                }
            }
        }
    }

    private static Type GetApplyerTypeFor((OneOf<FilteredEvents, AllEvents> Filter, object Transpose) subscription)
    {
        var transposeType = subscription.Transpose.GetType();
        var receiverType = transposeType.GetGenericArguments()[0];
        Type applyerType;
        if (transposeType.IsAssignableToGenericType(typeof(AggregateLoadingTranspose<>)))
        {
            applyerType = typeof(TransposeApplyer<>).MakeGenericType(new[] { receiverType });
        }
        else
        {
            if (transposeType.IsAssignableToGenericType(typeof(AggregateLoadingTranspose<,>)))
            {
                var eventType = transposeType.GetGenericArguments()[1];
                applyerType = typeof(TransposeApplyer<,>).MakeGenericType(new[] { receiverType, eventType });
            }
            else
            {
                throw new Exception($"Unsupported transpose type - '{transposeType.FullName}'");
            }
        }

        return applyerType;
    }

}