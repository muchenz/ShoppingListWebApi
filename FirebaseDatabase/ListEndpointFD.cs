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
        FirestoreDb Db;

        CollectionReference _listAggrCol;
        CollectionReference _listCol;
        CollectionReference _listItemCol;
        CollectionReference _invitationsCol;
        CollectionReference _userListAggrCol;
        CollectionReference _usersCol;
        CollectionReference _indexesCol;

        public ListEndpointFD(IMapper mapper)
        {
           
            Db = FirestoreDb.Create("testnosqldb1");
            _mapper = mapper;


            _listAggrCol = Db.Collection("listAggregator");
            _listCol = Db.Collection("list");
            _listItemCol = Db.Collection("listItem");
            _invitationsCol = Db.Collection("invitations");
            _userListAggrCol = Db.Collection("userListAggregator");
            _usersCol = Db.Collection("users");
            _indexesCol = Db.Collection("indexes");

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

        public async Task<int> DeleteListAsync(int listId, int listAggregationId)
        {
            Task<WriteResult> writeResultTask = null;
            
            await Db.RunTransactionAsync(async transation =>
            {

                var listDocSnap = await transation.Database.Collection("list").Document(listId.ToString()).GetSnapshotAsync();

                if (!listDocSnap.Exists) return;

                var listToDelete = listDocSnap.ConvertTo<ListFD>();

                var listAggrDocSnap = await transation.Database.Collection("listAggregator")
                        .Document(listToDelete.ListAggrId.ToString()).GetSnapshotAsync();

                var listArrgDoc = listAggrDocSnap.ConvertTo<ListAggregatorFD>();

                listArrgDoc.Lists.Remove(listId);


                var listTask = new List<Task>();

                foreach (var listItemId in listToDelete.ListItems)
                {
                    listTask.Add(transation.Database.Collection("listItem")
                        .Document(listItemId.ToString()).DeleteAsync());
                }


                var t1 =  transation.Database.Collection("listAggregator").Document(listToDelete.ListAggrId.ToString())
                        .UpdateAsync(nameof(ListAggregatorFD.Lists), listArrgDoc.Lists);
                listTask.Add(t1);

                writeResultTask = transation.Database.Collection("list").Document(listId.ToString()).DeleteAsync();

                listTask.Add(writeResultTask);

                await Task.WhenAll(listTask);
            });

            if (writeResultTask?.Result != null) return 1;

            return 0;
        }

        public async Task<List> EditListAsync(List list, int listAggregationId)
        {
            var listItemDocSnap = await _listCol.Document(list.ListId.ToString())
                   .UpdateAsync(nameof(ListFD.ListName), list.ListName);
            return list;
        }
    }
}
