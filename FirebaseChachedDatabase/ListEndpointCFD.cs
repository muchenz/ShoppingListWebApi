﻿using AutoMapper;
using FirebaseDatabase;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class ListEndpointCFD : IListEndpoint
    {
        private readonly ListEndpointFD _listEndpointFD;
        private readonly IDistributedCache _cache;

        public ListEndpointCFD(IMapper mapper, ListEndpointFD listEndpointFD, IDistributedCache cache) 
        {
            _listEndpointFD = listEndpointFD;
            _cache = cache;
        }

        public async Task<List> AddListAsync(int parentId, List list, int listAggregationId)
        {
            await _cache.RemoveAnyKeyAsync(listAggregationId);

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

            return await _listEndpointFD.DeleteListAsync(listId, listAggregationId);
        }

        public async Task<List> EditListAsync(List list)
        {
            await _cache.RemoveAnyKeyAsync(list.ListAggrId);

            return await _listEndpointFD.EditListAsync(list);
        }
    }

}
