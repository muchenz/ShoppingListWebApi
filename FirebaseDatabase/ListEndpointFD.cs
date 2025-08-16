using AutoMapper;
using Google.Cloud.Firestore;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseDatabase
{
    public class ListEndpointFD : IListEndpoint
    {

        private readonly IMapper _mapper;
        private readonly FirebaseFDOptions _firebaseFDOptions;
        private readonly DeleteChannel _deleteChannel;
        FirestoreDb Db;

        CollectionReference _listAggrCol;
        CollectionReference _listCol;
        CollectionReference _listItemCol;
        CollectionReference _invitationsCol;
        CollectionReference _userListAggrCol;
        CollectionReference _usersCol;
        CollectionReference _indexesCol;
        CollectionReference _toDelete;

        public ListEndpointFD(IMapper mapper, FirebaseFDOptions firebaseFDOptions, DeleteChannel deleteChannel)
        {
           
            Db = FirestoreDb.Create("testnosqldb1");
            _mapper = mapper;
            _firebaseFDOptions = firebaseFDOptions;
            _deleteChannel = deleteChannel;
            _listAggrCol = Db.Collection("listAggregator");
            _listCol = Db.Collection("list");
            _listItemCol = Db.Collection("listItem");
            _invitationsCol = Db.Collection("invitations");
            _userListAggrCol = Db.Collection("userListAggregator");
            _usersCol = Db.Collection("users");
            _indexesCol = Db.Collection("indexes");
            _toDelete = Db.Collection("toDelete");

        }



        public async Task<List> AddListAsync(int parentId, List list, int ListAggregationId)
        {
            var listFD = _mapper.Map<ListFD>(list);
            listFD.ListAggrId = parentId;
            //listFD.ListAggrId = listAggregationId;

            await Db.RunTransactionAsync(async transation =>
            {


                var docIndexesRef = _indexesCol.Document("indexes");
                var snapIndexesDoc = await transation.GetSnapshotAsync(docIndexesRef);

                var index = snapIndexesDoc.GetValue<long>("list");
                var indexNew = index + 1;

                var listAggrDocRef = _listAggrCol.Document(parentId.ToString());
                var listAggrDocSnap = await transation.GetSnapshotAsync(listAggrDocRef);

                if (!listAggrDocSnap.Exists)
                {
                    return;
                }

                var listAggr = listAggrDocSnap.ConvertTo<ListAggregatorFD>();
                listAggr.Lists.Add((int)indexNew);

                transation.Update(listAggrDocRef, nameof(ListAggregatorFD.Lists), listAggr.Lists);


                var newListRef = _listCol.Document((indexNew).ToString());

                listFD.ListId = (int)indexNew;


                transation.Set(newListRef, listFD);

                transation.Update(docIndexesRef, "list", indexNew);

            });


            return _mapper.Map<List>(listFD);
        }

        public Task<bool> CheckIntegrityListAggrAsync(int listAggrId, int listAggregationId)
        {
            return Task.FromResult(listAggrId == listAggregationId);
        }

        public async Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId)
        {
            var listDocSnap = await _listCol.Document(listId.ToString()).GetSnapshotAsync();

            if (!listDocSnap.Exists) return false;

            var list = listDocSnap.ConvertTo<ListFD>();
           
            return list.ListAggrId == listAggregationId;
        }

        //TODO: avoid limit 500 for transaction
        public async Task<int> DeleteListTransationAsync(int listId, int listAggregationId)
        {
            var amountDeleted = 0;

            await Db.RunTransactionAsync(async transation =>
            {

                var listDocRef = _listCol.Document(listId.ToString());
                var listDocSnap = await transation.GetSnapshotAsync(listDocRef);

                if (!listDocSnap.Exists) return;

                var listToDelete = listDocSnap.ConvertTo<ListFD>();

                var listAggrDocRef = _listAggrCol.Document(listToDelete.ListAggrId.ToString());
                transation.Update(listAggrDocRef, nameof(ListAggregatorFD.Lists), FieldValue.ArrayRemove(listId));
                
                foreach (var listItemId in listToDelete.ListItems)
                {
                    var _listItemToDeleteRef = _listItemCol.Document(listItemId.ToString());
                    transation.Delete(_listItemToDeleteRef);
                    amountDeleted++;
                }

                transation.Delete(listDocRef);
                amountDeleted++;
            });

            return amountDeleted;
        }

        public async Task<int> DeleteListAsync(int listId, int listAggregationId)
        {
            if (_firebaseFDOptions.UseBatchProcessing)
            {
                return await DeleteListBatchAsync(listId, listAggregationId);
            }

            return await DeleteListTransationAsync(listId, listAggregationId);
        }

        public async Task<int> DeleteListBatchAsync(int listId, int listAggregationId)
        {
            try
            {
                int amount = 0;
                await Db.RunTransactionAsync(async transation =>
                {
                    var todeleteRef = _toDelete.Document(nameof(List) + listId.ToString());

                    var listAggrDocRef = _listAggrCol.Document(listAggregationId.ToString());
                    transation.Update(listAggrDocRef, nameof(ListAggregatorFD.Lists), FieldValue.ArrayRemove(listId));

                    var toDedelete = new ToDelete
                    {
                        ItemToDeleteId = listId.ToString(),
                        Type = nameof(List),
                        CreatedAt = DateTime.UtcNow,
                    };

                    transation.Set(todeleteRef, toDedelete);
                    amount++;
                });

                await _deleteChannel.Writer.WriteAsync(new DeleteEvent());
                
                return amount;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine($"Error deleting list aggregator: {ex.Message}");
                return 0;
            }
        }

        public async Task<List> EditListAsync(List list, int listAggregationId)
        {
            var listItemDocSnap = await _listCol.Document(list.ListId.ToString())
                   .UpdateAsync(nameof(ListFD.ListName), list.ListName);
            return list;
        }
    }
}
