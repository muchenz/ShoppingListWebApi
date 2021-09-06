using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public interface IListAggregatorEndpoint
    {
        Task<ListAggregator> AddListAggregatorAsync(ListAggregator listAggregator, int parentId);
        Task<int> DeleteListAggrAsync(int listAggregationId);

        Task<ListAggregator> EditListAggregatorAsync(ListAggregator listAggregator);

        Task ChangeOrderListItemAsync(IEnumerable<ListAggregator> items);
    }
}
