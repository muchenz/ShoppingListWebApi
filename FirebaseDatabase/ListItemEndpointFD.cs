using AutoMapper;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Shared.DataEndpoints;
using Shared.DataEndpoints.Abstaractions;
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


                var docIndexesRef = transation.Database.Collection("indexes").Document("indexes");
                var snapIndexesDoc = await docIndexesRef.GetSnapshotAsync();

                var index = snapIndexesDoc.GetValue<long>("listItem");

                var listDocSnap = await transation.Database.Collection("list").Document(parentId.ToString()).GetSnapshotAsync();

                var lista = listDocSnap.ConvertTo<ListFD>();
                lista.ListItems.Add((int)index + 1);

                var t1 =  transation.Database.Collection("list").Document(parentId.ToString())
                        .UpdateAsync(nameof(ListFD.ListItems), lista.ListItems);


                var newListItemDoc = transation.Database.Collection("listItem").Document((index + 1).ToString());

                listItemFD.ListItemId = (int)index + 1;


                var t2 =  newListItemDoc.SetAsync(listItemFD);

                var t3 =  docIndexesRef.UpdateAsync("listItem", index + 1);

                await Task.WhenAll(t1, t2, t3);

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
            Task<WriteResult> writeResultTask = null;

            await Db.RunTransactionAsync(async transation =>
            {

                var listItemDocSnap = await transation.Database.Collection("listItem").Document(listItemId.ToString()).GetSnapshotAsync();

                if (!listItemDocSnap.Exists) return;

                var listItemToDelete = listItemDocSnap.ConvertTo<ListItemFD>();

                
                var listDocSnap = await transation.Database.Collection("list")
                        .Document(listItemToDelete.ListId.ToString()).GetSnapshotAsync();

                var listDoc = listDocSnap.ConvertTo<ListFD>();

                listDoc.ListItems.Remove(listItemId);

                var t1 =  transation.Database.Collection("list").Document(listItemToDelete.ListId.ToString())
                        .UpdateAsync(nameof(ListFD.ListItems), listDoc.ListItems);

                writeResultTask = transation.Database.Collection("listItem").Document(listItemId.ToString()).DeleteAsync();

                await Task.WhenAll(t1, writeResultTask);

                

            });

            if (writeResultTask?.Result != null) return 1;

            return 0;
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
