using AutoMapper;
using FirebaseDatabase;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class ListItemEndpointCFD : IListItemEndpoint
    {
        private readonly ListItemEndpointFD _listItemEndpointFD;
        private readonly IDistributedCache _cache;

        public ListItemEndpointCFD(IMapper mapper, ListItemEndpointFD listItemEndpointFD, IDistributedCache cache)
        {
            _listItemEndpointFD = listItemEndpointFD;
            _cache = cache;
        }

        public async Task<ListItem> AddListItemAsync(int parentId, ListItem listItem, int listAggregationId)
        {
            await _cache.RemoveAnyKeyAsync(listAggregationId);
            return await _listItemEndpointFD.AddListItemAsync(parentId, listItem, listAggregationId);
        }

        public Task<int> ChangeOrderListItemAsync(IEnumerable<ListItem> items)
        {
            return _listItemEndpointFD.ChangeOrderListItemAsync(items);
        }

        public Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId)
        {
            return _listItemEndpointFD.CheckIntegrityListAsync(listId, listAggregationId);
        }

        public Task<bool> CheckIntegrityListItemAsync(int listItemId, int listAggregationId)
        {
            return _listItemEndpointFD.CheckIntegrityListItemAsync(listItemId, listAggregationId);
        }

        public async Task<int> DeleteListItemAsync(int listItemId, int listAggregationId)
        {
            await _cache.RemoveAnyKeyAsync(listAggregationId);
            return await _listItemEndpointFD.DeleteListItemAsync(listItemId, listAggregationId);
        }
              

        public async Task<ListItem> EditListItemAsync(ListItem listItem)
        {
            await _cache.RemoveAnyKeyAsync(listItem.ListAggrId);

            return await _listItemEndpointFD.EditListItemAsync(listItem);
        }

        public Task<ListItem> GetItemListItemAsync(int listItemId)
        {
            return _listItemEndpointFD.GetItemListItemAsync(listItemId);
        }

        public async Task<ListItem> SavePropertyAsync(ListItem listItem, string propertyName, int listAggregationId)
        {
            //await _cache.RemoveAnyKeyAsync(listAggregationId);

            var res = await _cache.GetOrAddAsync(listAggregationId, _ => Task.FromResult(new ListAggregator()));

            if (res.Cached)
            {
                foreach (var list in res.value.Lists)
                {
                    foreach (var item in list.ListItems)
                    {

                        if (item.ListItemId == listItem.ListItemId)
                        {

                           var value = listItem.GetType().GetProperty(propertyName).GetValue(listItem);

                            item.GetType().GetProperty(propertyName).SetValue(item, value);

                            await _cache.SetAsync(listAggregationId, res.value);

                        }

                    }

                }
            }

            return await _listItemEndpointFD.SavePropertyAsync(listItem, propertyName, listAggregationId);
        }
    }

}
