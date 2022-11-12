﻿using Plants.Infrastructure.Domain.Helpers;

namespace Plants.Domain.Infrastructure;

internal static class InfrastructureHelpers
{
    private readonly static Lazy<AggregateHelper> _aggregateHelper = new(() => new AggregateHelper(Shared.Helpers.Type));
    public static AggregateHelper Aggregate => _aggregateHelper.Value;
}