﻿using Plants.Domain.Aggregate;

namespace Plants.Aggregates.PlantOrders;

public record StartOrderDeliveryCommand(CommandMetadata Metadata, string TrackingNumber) : Command(Metadata);
public record OrderDeliveryStartedEvent(EventMetadata Metadata, string TrackingNumber) : Event(Metadata);

public record RejectOrderCommand(CommandMetadata Metadata) : Command(Metadata);
public record RejectedOrderEvent(EventMetadata Metadata) : Event(Metadata);

public record ConfirmDeliveryCommand(CommandMetadata Metadata) : Command(Metadata);
public record DeliveryConfirmedEvent(EventMetadata Metadata, string SellerUsername, string GroupName, decimal Price) : Event(Metadata);