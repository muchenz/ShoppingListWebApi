using AutoMapper;
using FirebaseDatabase;
using Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class ListAggregatorEndpointCFD : IListAggregatorEndpoint
    {
        private readonly ListAggregatorEndpointFD _listAggregatorEndpoint;

        public ListAggregatorEndpointCFD(IMapper mapper, ListAggregatorEndpointFD listAggregatorEndpoint) 
        {
            _listAggregatorEndpoint = listAggregatorEndpoint;
        }

        public Task<ListAggregator> AddListAggregatorAsync(ListAggregator listAggregator, int parentId)
        {
            return _listAggregatorEndpoint.AddListAggregatorAsync(listAggregator, parentId);
        }

        public Task ChangeOrderListItemAsync(IEnumerable<ListAggregator> items)
        {
            return _listAggregatorEndpoint.ChangeOrderListItemAsync(items);
        }

        public Task<int> DeleteListAggrAsync(int listAggregationId)
        {
            return _listAggregatorEndpoint.DeleteListAggrAsync(listAggregationId);
        }

        public Task<ListAggregator> EditListAggregatorAsync(ListAggregator listAggregator)
        {
            return _listAggregatorEndpoint.EditListAggregatorAsync(listAggregator);
        }
    }

}
