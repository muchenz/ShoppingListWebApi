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
    public class ListAggregatorEndpointFD : IListAggregatorEndpoint
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
        CollectionReference _toDelete;

        private object listItemId;

        public ListAggregatorEndpointFD(IMapper mapper)
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
            _toDelete = Db.Collection("toDelete");

        }
        public async Task<ListAggregator> AddListAggregatorAsync(ListAggregator listAggregator, int parentId)
        {
            var listAggrFD = _mapper.Map<ListAggregatorFD>(listAggregator);

            
            var userListAggregatorFD = new UserListAggregatorFD { UserId = parentId,  PermissionLevel = 1 };

            await Db.RunTransactionAsync(async transation =>
            {


                var docIndexesRef = _indexesCol.Document("indexes");
                var snapIndexesDoc = await docIndexesRef.GetSnapshotAsync();

                var index = snapIndexesDoc.GetValue<long>("listAggregator");
                var indexNew = index + 1;
                listAggrFD.ListAggregatorId = (int)indexNew;
                
                var refDocListAggr = _listAggrCol.Document((index+1).ToString());
                transation.Set(refDocListAggr, listAggrFD);

                userListAggregatorFD.ListAggregatorId = (int)indexNew;
                var refUserListAggregatorFD = _userListAggrCol.Document();

                transation.Set(refUserListAggregatorFD, userListAggregatorFD);

                transation.Update(docIndexesRef, "listAggregator", indexNew);

            });

            var listAggr = _mapper.Map<ListAggregator>(listAggrFD);

            listAggr.PermissionLevel = 1;
            return listAggr;
        }

        public Task ChangeOrderListItemAsync(IEnumerable<ListAggregator> items)
        {
            throw new NotImplementedException();
        }

        public async Task<int> DeleteListAggrAsync(int listAggregationId)
        {
            try
            {
                int amount = 0;
                await Db.RunTransactionAsync(async transation =>
                {
                    var todeleteRef = _toDelete.Document(nameof(ListAggregator)+listAggregationId.ToString());

                    var listAggrRef = _listAggrCol.Document(listAggregationId.ToString());
                    var listAggrSnap = await transation.GetSnapshotAsync(listAggrRef);

                    if (!listAggrSnap.Exists) return;

                    transation.Update(listAggrRef, nameof(ListAggregatorFD.Deleted), true);

                    var toDedelete = new ToDelete
                    {
                        Id = listAggregationId.ToString(),
                        Type = nameof(ListAggregator),
                        CreatedAt = DateTime.UtcNow,
                    };

                    transation.Set(todeleteRef, toDedelete);
                    amount++;
                });

                return amount;
            }
            catch(Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine($"Error deleting list aggregator: {ex.Message}");
                return 0;
            }
        }
        //TODO: avoid 500 limit
        public async Task<int> DeleteListAggrAsync2(int listAggregationId)
        {
            try
            {
                int amount = 0;
                await Db.RunTransactionAsync(async transation =>
                {
                    var userListAggrRef = _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId);
                    var userListAggrSanp = await transation.GetSnapshotAsync(userListAggrRef);


                    var listAggrRef = _listAggrCol.Document(listAggregationId.ToString());
                    var listAggrSnap = await transation.GetSnapshotAsync(listAggrRef);

                    if (!listAggrSnap.Exists) return;

                    var listAggr = listAggrSnap.ConvertTo<ListAggregatorFD>();


                    var allLists = new List<ListFD>();

                    foreach (var listId in listAggr.Lists)
                    {
                        var listRef = _listCol.Document(listId.ToString());
                        var listSnap = await transation.GetSnapshotAsync(listRef);

                        if (!listSnap.Exists) continue;

                        var list = listSnap.ConvertTo<ListFD>();
                        allLists.Add(list);
                    }


                    foreach (var list in allLists)
                    {

                        foreach (var listItemId in list.ListItems)
                        {

                            var listItemRef = _listItemCol.Document(listItemId.ToString());

                            transation.Delete(listItemRef);

                            amount++;
                        }

                        transation.Delete(_listCol.Document(list.ListId.ToString()));
                        amount++;
                    }

                    transation.Delete(listAggrRef);
                    amount++;


                    foreach (var item in userListAggrSanp)
                    {
                        var userListAggrDocRef = _userListAggrCol.Document(item.Id);
                        transation.Delete(userListAggrDocRef);
                        amount++;
                    }

                });
                return amount;
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine($"Error deleting list aggregator: {ex.Message}");
                return 0;
            }
        }

        public async Task<ListAggregator> EditListAggregatorAsync(ListAggregator listAggregator)
        {
            var listAggrDocSnap = await _listAggrCol.Document(listAggregator.ListAggregatorId.ToString())
                  .UpdateAsync(nameof(ListAggregator.ListAggregatorName), listAggregator.ListAggregatorName);
            return listAggregator;
        }
    }
}
