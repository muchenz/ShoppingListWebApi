using AutoMapper;
using FirebaseDatabase;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace FirebaseChachedDatabase
{
    internal class ListAggregatorEndpointCFD : IListAggregatorEndpoint
    {
        private readonly ListAggregatorEndpointFD _listAggregatorEndpoint;
        private readonly CacheConveinient _cache;
        private readonly IMemoryCache _memoryCache;

        public ListAggregatorEndpointCFD(IMapper mapper, ListAggregatorEndpointFD listAggregatorEndpoint
            , CacheConveinient cache, IMemoryCache memoryCache)
        {
            _listAggregatorEndpoint = listAggregatorEndpoint;
            _cache = cache;
            _memoryCache = memoryCache;
        }

        public async Task<ListAggregator> AddListAggregatorAsync(ListAggregator listAggregator, int parentId)
        {
            var addedListAggr = await _listAggregatorEndpoint.AddListAggregatorAsync(listAggregator, parentId);

            var cashedListAggr = await _cache.GetAsync<List<UserListAggregator>>(Dictionary.UserId + parentId);

            if (cashedListAggr != null)
            {
                cashedListAggr.Add(new UserListAggregator
                {
                    ListAggregatorId = addedListAggr.ListAggregatorId,
                    UserId = parentId,
                    PermissionLevel = 1
                });
                await _cache.SetAsync(Dictionary.UserId+ parentId, cashedListAggr);
            }

            await _cache.SetAsync(addedListAggr.ListAggregatorId, addedListAggr);
            _memoryCache.Set(addedListAggr.ListAggregatorId, addedListAggr);
            return addedListAggr;



        }

        public Task ChangeOrderListItemAsync(IEnumerable<ListAggregator> items)
        {
            return _listAggregatorEndpoint.ChangeOrderListItemAsync(items);
        }

        public async Task<int> DeleteListAggrAsync(int listAggregationId)
        {
            var Db = FirestoreDb.Create("testnosqldb1");

            var userListAggrCol = Db.Collection("userListAggregator");

            var listUseListAggr = (await userListAggrCol.Database.Collection("userListAggregator")
              .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId)
              .GetSnapshotAsync()).Select(a => a.ConvertTo<UserListAggregatorFD>());

            var deleted = await _listAggregatorEndpoint.DeleteListAggrAsync(listAggregationId);

            await _cache.RemoveAnyKeyAsync(listAggregationId);
            _memoryCache.Remove(listAggregationId);


            foreach (var item in listUseListAggr)
            {
                var listUsAggr = await _cache.GetAsync<List<UserListAggregatorFD>>(Dictionary.UserId + item.UserId);

                var tempToDelete = listUsAggr?.Where(a => a.ListAggregatorId == listAggregationId)
                    .FirstOrDefault();

                if (tempToDelete != null)
                {
                    listUsAggr.Remove(tempToDelete);

                    await _cache.SetAsync(Dictionary.UserId + item.UserId, listUsAggr);

                }

            }

            return deleted;
        }

        public async Task<ListAggregator> EditListAggregatorAsync(ListAggregator listAggregator)
        {
            await _cache.RemoveAnyKeyAsync(listAggregator.ListAggregatorId);
            _memoryCache.Remove(listAggregator.ListAggregatorId);
            return await _listAggregatorEndpoint.EditListAggregatorAsync(listAggregator);
        }
    }

}
