using AutoMapper;
using FirebaseDatabase;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class ListAggregatorEndpointCFD : IListAggregatorEndpoint
    {
        private readonly ListAggregatorEndpointFD _listAggregatorEndpoint;
        private readonly IDistributedCache _cache;

        public ListAggregatorEndpointCFD(IMapper mapper, ListAggregatorEndpointFD listAggregatorEndpoint
            , IDistributedCache cache) 
        {
            _listAggregatorEndpoint = listAggregatorEndpoint;
            _cache = cache;
        }

        public async Task<ListAggregator> AddListAggregatorAsync(ListAggregator listAggregator, int parentId)
        {
            var addedListAggr = await _listAggregatorEndpoint.AddListAggregatorAsync(listAggregator, parentId);

            var cashedListAggr = await _cache.GetOrAddAsync("userId_" + parentId, i => Task.FromResult(new List<UserListAggregator>()));

            if (cashedListAggr.Cached)
            {
                cashedListAggr.value.Add(new UserListAggregator { ListAggregatorId=addedListAggr.ListAggregatorId,
                 UserId=parentId, PermissionLevel=1});
                await _cache.SetAsync("userId_" + parentId, cashedListAggr.value);
            }

            await _cache.SetAsync(addedListAggr.ListAggregatorId, addedListAggr);

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
           
            
            foreach (var item in listUseListAggr)
            {
                var listUsAggr = await _cache.GetOrAddAsync("userId_" + item.UserId, _=> Task.FromResult(
                    new List<UserListAggregatorFD>()));

                var tempToDelete = listUsAggr.value.Where(a => a.ListAggregatorId == listAggregationId)
                    .FirstOrDefault();

                if (tempToDelete != null)
                {
                    listUsAggr.value.Remove(tempToDelete);

                    await _cache.SetAsync("userId_" + item.UserId, listUsAggr.value);

                }

            }

            return deleted;
        }

        public async Task<ListAggregator> EditListAggregatorAsync(ListAggregator listAggregator)
        {
            await _cache.RemoveAnyKeyAsync(listAggregator.ListAggregatorId);

            return await _listAggregatorEndpoint.EditListAggregatorAsync(listAggregator);
        }
    }

}
