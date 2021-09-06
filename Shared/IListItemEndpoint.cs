using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public interface IListItemEndpoint
    {
        Task<ListItem> AddListItemAsync(int parentId, ListItem listItem, int listAggregationId);
        Task<int> DeleteListItemAsync(int listItemId);
        Task<ListItem> GetItemListItemAsync(int listItemId);
        Task<bool> CheckIntegrityListItemAsync(int listItemId, int listAggregationId);
        Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId);
        Task<ListItem> EditListItemAsync(ListItem listItem);
        Task<int> ChangeOrderListItemAsync(IEnumerable<ListItem> items);
        Task<ListItem> SavePropertyAsync(ListItem listItem, string propertyName);
    }
}
