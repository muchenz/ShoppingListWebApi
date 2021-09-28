using AutoMapper;
using Google.Cloud.Firestore;
using Shared;
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

        }
        public async Task<ListAggregator> AddListAggregatorAsync(ListAggregator listAggregator, int parentId)
        {
            var listAggrFD = _mapper.Map<ListAggregatorFD>(listAggregator);

            
            var userListAggregatorFD = new UserListAggregatorFD { UserId = parentId,  PermissionLevel = 1 };

            await Db.RunTransactionAsync(async transation =>
            {


                var docIndexesRef = transation.Database.Collection("indexes").Document("indexes");
                var snapIndexesDoc = await docIndexesRef.GetSnapshotAsync();

                var index = snapIndexesDoc.GetValue<long>("listAggregator");

                listAggrFD.ListAggregatorId = (int)index + 1;
                
                var refDocListAggr =  transation.Database.Collection("listAggregator").Document((index+1).ToString());
                var t1 = refDocListAggr.SetAsync(listAggrFD);

                userListAggregatorFD.ListAggregatorId = (int)index + 1;

                var t2 =  transation.Database.Collection("userListAggregator").AddAsync(userListAggregatorFD);

                var t3 = docIndexesRef.UpdateAsync("listAggregator", index + 1);

                await Task.WhenAll(t1, t2, t3);
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
            int amount = 0;
            await Db.RunTransactionAsync(async transation =>
            {

                var snapListAggr = await transation.Database.Collection("listAggregator")
                    .Document(listAggregationId.ToString()).GetSnapshotAsync();

                if (!snapListAggr.Exists) return;

                var listAggr = snapListAggr.ConvertTo<ListAggregatorFD>();


                foreach (var listId in listAggr.Lists)
                {
                    var snapList = await transation.Database.Collection("list")
                    .Document(listId.ToString()).GetSnapshotAsync();

                    var list = snapListAggr.ConvertTo<ListFD>();

                    var listItemTask = new List<Task>(); 
                    foreach (var listItemId in list.ListItems)
                    {
                        listItemTask.Add( transation.Database.Collection("listItem")
                            .Document(listItemId.ToString()).DeleteAsync());
                        amount++;
                    }
                    await Task.WhenAll(listItemTask);

                    await transation.Database.Collection("list").Document(listId.ToString()).DeleteAsync();
                    amount++;
                }

                await transation.Database.Collection("listAggregator").Document(listAggregationId.ToString()).DeleteAsync();
                amount++;

                var snapUseListAggr = await transation.Database.Collection("userListAggregator")
                 .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId).GetSnapshotAsync();


                foreach (var item in snapUseListAggr)
                {

                    await transation.Database.Collection("userListAggregator").Document(item.Id).DeleteAsync();
                    amount++;
                }

            });
            return amount;
        }

        public async Task<ListAggregator> EditListAggregatorAsync(ListAggregator listAggregator)
        {
            var listAggrDocSnap = await _listAggrCol.Document(listAggregator.ListAggregatorId.ToString())
                  .UpdateAsync(nameof(ListAggregator.ListAggregatorName), listAggregator.ListAggregatorName);
            return listAggregator;
        }
    }
}
