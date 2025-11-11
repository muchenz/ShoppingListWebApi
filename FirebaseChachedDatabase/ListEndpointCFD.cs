using AutoMapper;
using FirebaseDatabase;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    internal class ListEndpointCFD : IListEndpoint
    {
        private readonly ListEndpointFD _listEndpointFD;
        private readonly CacheConveinient _cache;
        private readonly IMemoryCache _memoryCache;

        public ListEndpointCFD(IMapper mapper, ListEndpointFD listEndpointFD, CacheConveinient cache, IMemoryCache memoryCache) 
        {
            _listEndpointFD = listEndpointFD;
            _cache = cache;
            _memoryCache = memoryCache;
        }

        public async Task<List> AddListAsync(int parentId, List list, int listAggregationId)
        {
            await _cache.RemoveAnyKeyAsync(listAggregationId);
            _memoryCache.Remove(listAggregationId);
            return await _listEndpointFD.AddListAsync(parentId, list, listAggregationId);
        }

        public Task<bool> CheckIntegrityListAggrAsync(int listAggrId, int listAggregationId)
        {
            return _listEndpointFD.CheckIntegrityListAggrAsync(listAggrId, listAggregationId);
        }

        public Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId)
        {
            return _listEndpointFD.CheckIntegrityListAsync(listId, listAggregationId);
        }

        public async Task<int> DeleteListAsync(int listId, int listAggregationId)
        {
            await _cache.RemoveAnyKeyAsync(listAggregationId);
            _memoryCache.Remove(listAggregationId);

            return await _listEndpointFD.DeleteListAsync(listId, listAggregationId);
        }

        public async Task<List> EditListAsync(List list, int listAggregationId)
        {
            await _cache.RemoveAnyKeyAsync(listAggregationId);
            _memoryCache.Remove(listAggregationId);

            return await _listEndpointFD.EditListAsync(list, listAggregationId);
        }
    }

}
