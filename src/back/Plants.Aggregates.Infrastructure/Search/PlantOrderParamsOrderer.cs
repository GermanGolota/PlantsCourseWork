﻿using Nest;
using Plants.Aggregates.PlantOrders;
using Plants.Aggregates.Search;

namespace Plants.Aggregates.Infrastructure.Search;

internal class PlantOrderParamsOrderer : ISearchParamsOrderer<PlantOrder, PlantOrderParams>
{
    public IPromise<IList<ISort>> OrderParams(PlantOrderParams parameters, SortDescriptor<PlantOrder> desc) =>
        desc.Field(_ => _.Field(agg => agg.Status).Ascending());
}
