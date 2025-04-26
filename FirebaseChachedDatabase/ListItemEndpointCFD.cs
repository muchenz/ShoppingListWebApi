using AutoMapper;
using FirebaseDatabase;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Shared.DataEndpoints;
using Shared.DataEndpoints.Abstaractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class ListItemEndpointCFD : IListItemEndpoint
    {
        private readonly ListItemEndpointFD _listItemEndpointFD;
        private readonly CacheConveinient _cache;

        public ListItemEndpointCFD(IMapper mapper, ListItemEndpointFD listItemEndpointFD, CacheConveinient cache)
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
            // await _cache.RemoveAnyKeyAsync(listAggregationId);

            await DeleteLiteItem(listItemId, listAggregationId);

            return await _listItemEndpointFD.DeleteListItemAsync(listItemId, listAggregationId);
        }


        public async Task<ListItem> EditListItemAsync(ListItem listItem, int listAggregationId)
        {
            await ChangePropertyNameOfListItem(listItem, nameof(ListItem.ListItemName), listAggregationId);

            return await _listItemEndpointFD.EditListItemAsync(listItem, listAggregationId);
        }

        public Task<ListItem> GetItemListItemAsync(int listItemId)
        {
            return _listItemEndpointFD.GetItemListItemAsync(listItemId);
        }

        public async Task<ListItem> SavePropertyAsync(ListItem listItem, string propertyName, int listAggregationId)
        {
            //await _cache.RemoveAnyKeyAsync(listAggregationId);

            await ChangePropertyNameOfListItem(listItem, propertyName, listAggregationId);

            return await _listItemEndpointFD.SavePropertyAsync(listItem, propertyName, listAggregationId);
        }

        private async Task ChangePropertyNameOfListItem(ListItem listItem, string propertyName, int listAggregationId)
        {
            var res = await _cache.GetAsync<ListAggregator>(listAggregationId);

            if (res != null)
            {
                foreach (var list in res.Lists)
                {
                    foreach (var item in list.ListItems)
                    {

                        if (item.ListItemId == listItem.ListItemId)
                        {

                            var value = listItem.GetType().GetProperty(propertyName).GetValue(listItem);

                            item.GetType().GetProperty(propertyName).SetValue(item, value);

                            await _cache.SetAsync(listAggregationId, res);

                        }

                    }

                }
            }
        }


        private async Task DeleteLiteItem(int listItemId, int listAggregationId)
        {
            var res = await _cache.GetAsync<ListAggregator>(listAggregationId);

            ListItem todelete = null;
            List fromdelete = null;
            if (res != null)
            {
                foreach (var item in res.Lists)
                {

                    todelete = item.ListItems.FirstOrDefault(b => b.ListItemId == listItemId);
                    fromdelete = item;

                    if (todelete != null && fromdelete != null)
                    {
                        fromdelete.ListItems.Remove(todelete);

                        await _cache.SetAsync(listAggregationId, res);

                    }
                }
            }
        }
    }

}


