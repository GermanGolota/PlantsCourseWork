﻿namespace Plants.Aggregates;

[Allow(Consumer, Read)]
[Allow(Producer, Read)]
[Allow(Producer, Write)]
[Allow(Manager, Read)]
[Allow(Manager, Write)]
public class PlantInstruction : AggregateBase,
    IEventHandler<InstructionCreatedEvent>, IEventHandler<InstructionEditedEvent>
{
    public PlantInstruction(Guid id) : base(id)
    {
    }

    public InstructionModel Information { get; private set; }
    public Picture Cover { get; private set; }

    public void Handle(InstructionCreatedEvent @event)
    {
        Information = @event.Instruction;
        Cover = new(Guid.NewGuid(), @event.CoverUrl);
    }

    public void Handle(InstructionEditedEvent @event)
    {
        Information = @event.Instruction;
        if (@event.CoverUrl is not null)
        {
            Cover = new(Guid.NewGuid(), @event.CoverUrl);
        }
    }

}