using AutoMapper;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseDatabase
{
    public class ListItemEndpointFD : IListItemEndpoint
    {
        private readonly IMapper _mapper;
        FirestoreDb Db;

        CollectionReference _listCol;
        CollectionReference _listItemCol;

        public ListItemEndpointFD(IMapper mapper)
        {
           
            Db = FirestoreDb.Create("testnosqldb1");
            _mapper = mapper;


            _listCol = Db.Collection("list");
            _listItemCol = Db.Collection("listItem");

        }
        public async Task<ListItem> AddListItemAsync(int parentId, ListItem listItem, int listAggregationId)
        {
            var listItemFD = _mapper.Map<ListItemFD>(listItem);
            listItemFD.ListId = parentId;
            listItemFD.ListAggrId = listAggregationId;

            await Db.RunTransactionAsync(async transation =>
            {


                var docIndexesRef = Db.Collection("indexes").Document("indexes");
                var snapIndexesDoc = await transation.GetSnapshotAsync(docIndexesRef);

                var index = snapIndexesDoc.GetValue<long>("listItem");

                var indexNew = index + 1;
                var listDocRef = _listCol.Document(parentId.ToString());
                var listDocSnap = await transation.GetSnapshotAsync(listDocRef);

                if (!listDocSnap.Exists)
                {
                    return;
                }

                var lista = listDocSnap.ConvertTo<ListFD>();

                lista.ListItems.Add((int)indexNew);

                transation.Set(listDocRef, lista);


                var newListItemRef = _listItemCol.Document(indexNew.ToString());

                listItemFD.ListItemId = (int)indexNew;


                transation.Set(newListItemRef, listItemFD);

                transation.Update(docIndexesRef,"listItem", indexNew);

            });


            return _mapper.Map<ListItem>(listItemFD);
        }

        public async Task<int> ChangeOrderListItemAsync(IEnumerable<ListItem> items)
        {
            int amount = 0;
            foreach (var item in items)
            {
                var listItemDocSnap = await _listItemCol.Document(item.ListItemId.ToString())
                       .UpdateAsync(nameof(ListItemFD.Order), item.Order);
                amount++;
            }

            return amount;
        }

        public async Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId)
        {
            var listDocSnap = await _listCol.Document(listId.ToString()).GetSnapshotAsync();
            
            if (!listDocSnap.Exists) return false;

            var list = listDocSnap.ConvertTo<ListFD>();
            
            return list.ListAggrId == listAggregationId;
        }

        public async Task<bool> CheckIntegrityListItemAsync(int listItemId, int listAggregationId)
        {
            var listItemDocSnap = await _listItemCol.Document(listItemId.ToString()).GetSnapshotAsync();
            
            if (!listItemDocSnap.Exists) return false;
            
            var listItem = listItemDocSnap.ConvertTo<ListItemFD>();
                     

            return listItem.ListAggrId == listAggregationId;
        }

        public async Task<int> DeleteListItemAsync(int listItemId, int listAggregationId)
        {

            await Db.RunTransactionAsync(async transation =>
            {

                var listItemDocRef = _listItemCol.Document(listItemId.ToString());
                var listItemDocSnap = await transation.GetSnapshotAsync(listItemDocRef);

                if (!listItemDocSnap.Exists)
                {
                    return;
                }

                var listItemToDelete = listItemDocSnap.ConvertTo<ListItemFD>();

                var listDocSnapRef = _listCol.Document(listItemToDelete.ListId.ToString());
                var listDocSnap = await transation.GetSnapshotAsync(listDocSnapRef);

                var listDoc = listDocSnap.ConvertTo<ListFD>();

                listDoc.ListItems.Remove(listItemId);

                transation.Update(listDocSnapRef, nameof(ListFD.ListItems), listDoc.ListItems); 

                transation.Delete(listItemDocRef);
                

            });

            return 1;

        }




        public async Task<ListItem> EditListItemAsync(ListItem listItem, int listAggregationId)
        {
            var listItemDocSnap = await _listItemCol.Document(listItem.ListItemId.ToString())
                    .UpdateAsync(nameof(ListItemFD.ListItemName), listItem.ListItemName);
            return listItem;
        }

        public async Task<ListItem> GetItemListItemAsync(int listItemId)
        {

            var listItemDocSnap = await _listItemCol.Document(listItemId.ToString()).GetSnapshotAsync();

            var listItem = listItemDocSnap.ConvertTo<ListItemFD>();

            return _mapper.Map<ListItem>(listItem);
        }

        public async Task<ListItem> SavePropertyAsync(ListItem listItem, string propertyName, int listAggregationId)
        {
            var listItemDocSnap = await _listItemCol.Document(listItem.ListItemId.ToString())
                .UpdateAsync(propertyName, listItem.GetType().GetProperty(propertyName).GetValue(listItem));

            return listItem;
        }
    }
}
