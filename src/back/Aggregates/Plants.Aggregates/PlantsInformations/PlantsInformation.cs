﻿namespace Plants.Aggregates;

[Allow(Consumer, Read)]
[Allow(Producer, Read)]
[Allow(Producer, Write)]
[Allow(Manager, Read)]
[Allow(Manager, Write)]
public class PlantsInformation : AggregateBase, IEventHandler<StockAddedEvent>,
    IEventHandler<InstructionCreatedEvent>, IEventHandler<StockItemPostedEvent>,
    IEventHandler<DeliveryConfirmedEvent>
{
    //Id that is being used by plant info singleton
    public static Guid InfoId { get; } = Guid.Parse("1eebef8d-ba56-406f-a9f5-bc21c1a9ca96");
    public PlantsInformation(Guid id) : base(InfoId)
    {
        if (id != Guid.Empty && id != InfoId)
        {
            throw new InvalidOperationException("Cannot use non-main plant info aggregate");
        }
    }

    public HashSet<string> GroupNames { get; private set; } = new();
    public HashSet<string> RegionNames { get; private set; } = new();
    public HashSet<string> SoilNames { get; private set; } = new();

    // group name
    public Dictionary<string, PlantStats> TotalStats { get; private set; } = new();
    // date yyyy-mm-dd, group name
    public Dictionary<string, Dictionary<string, PlantStats>> DailyStats { get; private set; } = new();

    public void Handle(StockAddedEvent @event)
    {
        var plant = @event.Plant;

        foreach (var group in plant.GroupNames)
        {
            GroupNames.Add(group);
            UpdateStats(group, @event.Metadata.Time, stat =>
            {
                stat.PlantsCount++;
                return stat;
            });
        }

        foreach (var soil in plant.SoilNames)
        {
            SoilNames.Add(soil);
        }

        foreach (var regionName in plant.RegionNames)
        {
            RegionNames.Add(regionName);
        }

    }

    public void Handle(InstructionCreatedEvent @event)
    {
        UpdateStats(@event.Instruction.GroupName, @event.Metadata.Time, stat =>
        {
            stat.InstructionsCount++;
            return stat;
        });
    }

    public void Handle(StockItemPostedEvent @event)
    {
        foreach (var group in @event.GroupNames)
        {
            UpdateStats(group, @event.Metadata.Time, stat =>
            {
                stat.PostedCount++;
                return stat;
            });
        }
    }

    public void Handle(DeliveryConfirmedEvent @event)
    {
        foreach (var group in @event.GroupNames)
        {
            UpdateStats(group, @event.Metadata.Time, stat =>
            {
                stat.SoldCount++;
                stat.Income += @event.Price;
                return stat;
            });
        }
    }

    private void UpdateStats(string groupName, DateTime time, Func<PlantStats, PlantStats> statUpdater)
    {
        if (TotalStats.ContainsKey(groupName) is false)
        {
            TotalStats[groupName] = new();
        }

        TotalStats[groupName] = statUpdater(TotalStats[groupName]);

        var date = GetDateKey(time);
        if (DailyStats.ContainsKey(date) is false)
        {
            DailyStats[date] = new();
        }

        var groupStats = DailyStats[date];
        if (groupStats.ContainsKey(groupName) is false)
        {
            groupStats[groupName] = new();
        }

        DailyStats[date][groupName] = statUpdater(groupStats[groupName]);
    }

    private static string GetDateKey(DateTime time) =>
        time.ToString("yyyy-MM-dd");

    private class PlantStockSubscription : IAggregateSubscription<PlantsInformation, PlantStock>
    {
        public IEnumerable<EventSubscriptionBase<PlantsInformation, PlantStock>> Subscriptions => new EventSubscriptionBase<PlantsInformation, PlantStock>[]
        {
            new EventSubscription<PlantsInformation, PlantStock, StockAddedEvent>(
                new AggregateLoadingTranspose<PlantsInformation, StockAddedEvent>(
                    _ => InfoId,
                    (oldEvents, info) =>
                        oldEvents.Select(added => info.TransposeSubscribedEvent(added)))
                ),
            new EventSubscription<PlantsInformation, PlantStock, StockItemPostedEvent>(
                new AggregateLoadingTranspose<PlantsInformation, StockItemPostedEvent>(
                    _ => InfoId,
                    (oldEvents, info) =>
                        oldEvents.Select(added => info.TransposeSubscribedEvent(added)))
                )
        };
    }

    private class PlantInstructionSubscription : IAggregateSubscription<PlantsInformation, PlantInstruction>
    {
        public IEnumerable<EventSubscriptionBase<PlantsInformation, PlantInstruction>> Subscriptions => new EventSubscriptionBase<PlantsInformation, PlantInstruction>[]
        {
            new EventSubscription<PlantsInformation, PlantInstruction, InstructionCreatedEvent>(
                new AggregateLoadingTranspose<PlantsInformation, InstructionCreatedEvent>(
                    _ => InfoId,
                    (oldEvents, info) =>
                        oldEvents.Select(added => info.TransposeSubscribedEvent(added)))
                )
        };
    }

    private class PlantOrderSubscription : IAggregateSubscription<PlantsInformation, PlantOrder>
    {
        public IEnumerable<EventSubscriptionBase<PlantsInformation, PlantOrder>> Subscriptions => new EventSubscriptionBase<PlantsInformation, PlantOrder>[]
        {
            new EventSubscription<PlantsInformation, PlantOrder, DeliveryConfirmedEvent>(
                new AggregateLoadingTranspose<PlantsInformation, DeliveryConfirmedEvent>(
                    _ => InfoId,
                    (oldEvents, info) =>
                        oldEvents.Select(added => info.TransposeSubscribedEvent(added)))
                )
        };
    }
}

public class PlantStats
{
    public long PlantsCount { get; set; }
    public long InstructionsCount { get; set; }
    public long PostedCount { get; set; }
    public long SoldCount { get; set; }
    public decimal Income { get; set; }
}
