﻿namespace Plants.Domain.Infrastructure;

internal class TransposeApplyer<TIn> where TIn : AggregateBase
{
    private readonly IQueryService<TIn> _repo;

    public TransposeApplyer(IQueryService<TIn> repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<Event>> CallTransposeAsync(AggregateLoadingTranspose<TIn> transpose, IEnumerable<Event> events, CancellationToken token = default)
    {
        var id = transpose.ExtractId(events.First());
        var aggregate = await _repo.GetByIdAsync(id, token: token);
        return transpose.Transpose(events, aggregate);
    }
}

internal class TransposeApplyer<TIn, TEvent> where TIn : AggregateBase where TEvent : Event
{
    private readonly IQueryService<TIn> _repo;

    public TransposeApplyer(IQueryService<TIn> repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<Event>> CallTransposeAsync(AggregateLoadingTranspose<TIn, TEvent> transpose, IEnumerable<Event> events, CancellationToken token = default)
    {
        var filteredEvents = events.OfType<TEvent>();

        return filteredEvents.Any()
                ? await ProcessFiltered(transpose, filteredEvents, token)
                : Array.Empty<Event>();
    }

    private async Task<IEnumerable<Event>> ProcessFiltered(AggregateLoadingTranspose<TIn, TEvent> transpose, IEnumerable<TEvent> filteredEvents, CancellationToken token)
    {
        var id = transpose.ExtractId(filteredEvents.First());
        var aggregate = await _repo.GetByIdAsync(id, token: token);
        return transpose.Transpose(filteredEvents, aggregate);
    }
}
