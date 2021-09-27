using AutoMapper;
using FirebaseDatabase;
using Shared;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class ListEndpointCFD : IListEndpoint
    {
        private readonly ListEndpointFD _listEndpointFD;

        public ListEndpointCFD(IMapper mapper, ListEndpointFD listEndpointFD) 
        {
            _listEndpointFD = listEndpointFD;
        }

        public Task<List> AddListAsync(int parentId, List list, int ListAggregationId)
        {
            return _listEndpointFD.AddListAsync(parentId, list, ListAggregationId);
        }

        public Task<bool> CheckIntegrityListAggrAsync(int listAggrId, int listAggregationId)
        {
            return _listEndpointFD.CheckIntegrityListAggrAsync(listAggrId, listAggregationId);
        }

        public Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId)
        {
            return _listEndpointFD.CheckIntegrityListAsync(listId, listAggregationId);
        }

        public Task<int> DeleteListAsync(int listId, int listAggregationId)
        {
            return _listEndpointFD.DeleteListAsync(listId, listAggregationId);
        }

        public Task<List> EditListAsync(List list)
        {
            return _listEndpointFD.EditListAsync(list);
        }
    }

}
