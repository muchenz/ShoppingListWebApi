using AutoMapper;
using FirebaseDatabase;
using Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class ListItemEndpointCFD : IListItemEndpoint
    {
        private readonly ListItemEndpointFD _listItemEndpointFD;

        public ListItemEndpointCFD(IMapper mapper, ListItemEndpointFD listItemEndpointFD)
        {
            _listItemEndpointFD = listItemEndpointFD;
        }

        public Task<ListItem> AddListItemAsync(int parentId, ListItem listItem, int listAggregationId)
        {
            return _listItemEndpointFD.AddListItemAsync(parentId, listItem, listAggregationId);
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

        public Task<int> DeleteListItemAsync(int listItemId)
        {
            return _listItemEndpointFD.DeleteListItemAsync(listItemId);
        }

        public Task<ListItem> EditListItemAsync(ListItem listItem)
        {
            return _listItemEndpointFD.EditListItemAsync(listItem);
        }

        public Task<ListItem> GetItemListItemAsync(int listItemId)
        {
            return _listItemEndpointFD.GetItemListItemAsync(listItemId);
        }

        public Task<ListItem> SavePropertyAsync(ListItem listItem, string propertyName)
        {
            return _listItemEndpointFD.SavePropertyAsync(listItem, propertyName);
        }
    }

}
