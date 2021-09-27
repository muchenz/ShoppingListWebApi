﻿using AutoMapper;
using FirebaseDatabase;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class UserEndpointCFD : IUserEndpoint
    {

        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly UserEndpointFD _userEndpointFD;
        FirestoreDb Db;

        CollectionReference _listAggrCol;
        CollectionReference _listCol;
        CollectionReference _listItemCol;
        CollectionReference _invitationsCol;
        CollectionReference _userListAggrCol;
        CollectionReference _usersCol;
        CollectionReference _indexesCol;


        public UserEndpointCFD(IMapper mapper, IDistributedCache cache, UserEndpointFD userEndpointFD)
        {
            Db = FirestoreDb.Create("testnosqldb1");
            _mapper = mapper;
            _cache = cache;
            _userEndpointFD = userEndpointFD;
            _listAggrCol = Db.Collection("listAggregator");
            _listCol = Db.Collection("list");
            _listItemCol = Db.Collection("listItem");
            _invitationsCol = Db.Collection("invitations");
            _userListAggrCol = Db.Collection("userListAggregator");
            _usersCol = Db.Collection("users");
            _indexesCol = Db.Collection("indexes");
        }



        async Task<(User, List<UserListAggregatorFD>)> GetUserAggrListAsync(string userName)
        {
            var aa = new ListAggregatorFD() { };


            var querrySnapshot = await _usersCol.WhereEqualTo("EmailAddress", userName).GetSnapshotAsync();

            var documentSnapshot = querrySnapshot.FirstOrDefault();
            var userFD = documentSnapshot.ConvertTo<UserFD>();
            userFD.Id = documentSnapshot.Id;

            //--------
            var userDTO = new User
            {
                EmailAddress = userName,
                LoginType = userFD.LoginType,
                Roles = userFD.Roles
                                    ,
                UserId = int.Parse(userFD.Id)
            };

            querrySnapshot = await _userListAggrCol.WhereEqualTo("UserId", userFD.UserId).GetSnapshotAsync();


            var listUserListAggregator = new List<UserListAggregatorFD>();

            foreach (var item in querrySnapshot)
            {
                var temp = item.ConvertTo<UserListAggregatorFD>();
                listUserListAggregator.Add(temp);
            }

            return (userDTO, listUserListAggregator);
        }


        async Task<List<ListAggregator>> GetListAggrDTO(IEnumerable<UserListAggregatorFD> listUserListAggregator)
        {



            var listListAggregatortDTO = new List<ListAggregator>();

            var docListAggerSnapshotsList = await RestrictQuerryListTo10Async(
                    listUserListAggregator.Select(a => a.ListAggregatorId),
                    (i, list) =>
                    _listAggrCol.WhereIn(nameof(ListAggregatorFD.ListAggregatorId),
                         list.Skip(10 * i).Take(10)).GetSnapshotAsync()
                    );


            foreach (var listAggrSanapTask in docListAggerSnapshotsList.Reverse())
            {


                var tempListAggrFD = listAggrSanapTask.ConvertTo<ListAggregatorFD>();

                var tempListAggrDTO = new ListAggregator
                {
                    ListAggregatorId = tempListAggrFD.ListAggregatorId,
                    ListAggregatorName = tempListAggrFD.ListAggregatorName
                ,
                    PermissionLevel = listUserListAggregator
                    .Single(a => a.ListAggregatorId == tempListAggrFD.ListAggregatorId).PermissionLevel
                };

                listListAggregatortDTO.Add(tempListAggrDTO);



                if (!tempListAggrFD.Lists.Any()) continue;




                var docListSnapshotsList = await RestrictQuerryListTo10Async(
                       tempListAggrFD.Lists.Select(a => a),
                       (i, list) =>
                       _listCol.WhereIn(nameof(ListFD.ListId),
                            list.Skip(10 * i).Take(10)).GetSnapshotAsync()
                       );


                foreach (var listSnapTask in docListSnapshotsList.Reverse())
                {


                    var tempListFD = listSnapTask.ConvertTo<ListFD>();

                    var tempListDTO = new List
                    {
                        ListId = int.Parse(listSnapTask.Id),
                        ListName = tempListFD.ListName,
                        Order = tempListFD.Order,
                        ListAggrId = tempListAggrDTO.ListAggregatorId

                    };

                    tempListAggrDTO.Lists.Add(tempListDTO);


                    if (!tempListFD.ListItems.Any()) continue;


                    var docListItemSnapshotsList = await RestrictQuerryListTo10Async(
                        tempListFD.ListItems.Select(a => a),
                        (i, list) =>
                        _listItemCol.WhereIn(nameof(ListItemFD.ListItemId),
                             list.Skip(10 * i).Take(10)).GetSnapshotAsync()
                        );


                    foreach (var listItemSanpTask in docListItemSnapshotsList.Reverse())
                    {

                        var tempListItemFD = listItemSanpTask.ConvertTo<ListItemFD>();

                        var tempListItemDTO = new ListItem
                        {
                            ListItemId = int.Parse(listItemSanpTask.Id),
                            ListItemName = tempListItemFD.ListItemName,
                            Order = tempListItemFD.Order,
                            State = tempListItemFD.State,
                            ListAggrId = tempListAggrDTO.ListAggregatorId

                        };

                        tempListDTO.ListItems.Add(tempListItemDTO);
                    }


                }

            }
            //userDTO.ListAggregators = listListAggregatortDTO;

            return listListAggregatortDTO;



        }

        async Task<IEnumerable<DocumentSnapshot>> RestrictQuerryListTo10Async(IEnumerable<int> argumentList, Func<int, IEnumerable<int>, Task<QuerySnapshot>> func)
        {

            int index = (int)Math.Ceiling(argumentList.Count() / 10.0);

            var listQuerrySnapTask = new List<Task<QuerySnapshot>>();


            for (int i = 0; i < index; i++)
            {
                listQuerrySnapTask.Add(func(i, argumentList));

            }

            await Task.WhenAll(listQuerrySnapTask);

            var documentSnapshotsList = listQuerrySnapTask.SelectMany(a => a.Result);

            return documentSnapshotsList;
        }

        public async  Task<User> GetTreeAsync(string userName)
        {

            var listDTO = new List<ListAggregator>();

            var (user, userAggrList) = await GetUserAggrListAsync(userName);

            var listToRemove = new List<UserListAggregatorFD>();

            foreach (var item in userAggrList)
            {

                var cashed = await _cache.GetOrAddAsync(item.ListAggregatorId, i => Task.FromResult(new ListAggregator()));

                if (cashed.Cached)
                {
                    listToRemove.Add(item);
                    listDTO.Add(cashed.value);
                }
            }

            listToRemove.ForEach(a => userAggrList.Remove(a));

            var listLackDataFromDatabaseDTO = await GetListAggrDTO(userAggrList);

            foreach (var item in listLackDataFromDatabaseDTO)
            {
                await _cache.SetAsync(item.ListAggregatorId, item);
            }

            listDTO.AddRange(listLackDataFromDatabaseDTO);

            user.ListAggregators = listDTO;

            return user;
        }

        public Task<User> FindUserByIdAsync(int id)
        {
            return _userEndpointFD.FindUserByIdAsync(id);
        }

        public Task<User> GetUserByNameAsync(string userName)
        {
            return _userEndpointFD.GetUserByNameAsync(userName);
        }

        public Task<bool> IsUserHasListAggregatorAsync(int userId, int listAggregatorId)
        {
            return _userEndpointFD.IsUserHasListAggregatorAsync(userId, listAggregatorId);
        }

        public Task AddUserListAggregationAsync(int userId, int listAggregationId, int permission)
        {
            return _userEndpointFD.AddUserListAggregationAsync(userId, listAggregationId, permission);
        }

        public Task<bool> IsUserInvitatedToListAggregationAsync(string userName, int listAggregationId)
        {
            return _userEndpointFD.IsUserInvitatedToListAggregationAsync(userName, listAggregationId);
        }

        public Task AddInvitationAsync(string toUserName, int listAggregationId, int permission, string fromSenderName)
        {
            return _userEndpointFD.AddInvitationAsync(toUserName, listAggregationId, permission, fromSenderName);
        }

        public Task<int> GetNumberOfAdministratorsOfListAggregationsAsync(int listAggregationId)
        {
            return _userEndpointFD.GetNumberOfAdministratorsOfListAggregationsAsync(listAggregationId);
        }

        public Task<int> GetLastAdminIdAsync(int listAggregationId)
        {
            return _userEndpointFD.GetLastAdminIdAsync(listAggregationId);
        }

        public Task SetUserPermissionToListAggrAsync(int userId, int listAggregationId, int permission)
        {
            return _userEndpointFD.SetUserPermissionToListAggrAsync(userId, listAggregationId, permission);
        }

        public Task DeleteUserListAggrAscync(int userId, int listAggregationId)
        {
            return _userEndpointFD.DeleteUserListAggrAscync(userId, listAggregationId);
        }

        public Task<List<ListAggregationForPermission>> GetListAggregationForPermission(string userName)
        {
            return _userEndpointFD.GetListAggregationForPermission(userName);
        }

        public Task<List<ListAggregationForPermission>> GetListAggregationForPermission2(string userName)
        {
            return _userEndpointFD.GetListAggregationForPermission2(userName);
        }

        public Task<List<UserListAggregator>> GetUserListAggrByUserId(int userId)
        {
            return _userEndpointFD.GetUserListAggrByUserId(userId);
        }

        public Task<List<string>> GetUserRolesByUserIdAsync(int userId)
        {
            return _userEndpointFD.GetUserRolesByUserIdAsync(userId);
        }

        public Task<User> GetUserWithRolesAsync(int userId)
        {
            return _userEndpointFD.GetUserWithRolesAsync(userId);
        }

        public Task<User> LoginAsync(string userName, string password)
        {
            return _userEndpointFD.LoginAsync(userName, password);
        }

        public Task<User> Register(string userName, string password, LoginType loginType)
        {
            return _userEndpointFD.Register(userName, password, loginType);
        }
    }

}
