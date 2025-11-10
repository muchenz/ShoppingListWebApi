using AutoMapper;
using FirebaseDatabase;
using Google.Apis;
using Google.Cloud.Firestore;
using Google.Type;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class UserEndpointCFD : IUserEndpoint, ITokenEndpoint
    {

        private readonly IMapper _mapper;
        private readonly CacheConveinient _cache;
        private readonly UserEndpointFD _userEndpointFD;
        private readonly IMemoryCache _memoryCache;
        FirestoreDb Db;

        CollectionReference _listAggrCol;
        CollectionReference _listCol;
        CollectionReference _listItemCol;
        CollectionReference _invitationsCol;
        CollectionReference _userListAggrCol;
        CollectionReference _usersCol;
        CollectionReference _indexesCol;
        CollectionReference _refreshToken;


        public UserEndpointCFD(IMapper mapper, CacheConveinient cache, UserEndpointFD userEndpointFD, IMemoryCache  memoryCache)
        {
            Db = FirestoreDb.Create("testnosqldb1");
            _mapper = mapper;
            _cache = cache;
            _userEndpointFD = userEndpointFD;
            _memoryCache = memoryCache;
            _listAggrCol = Db.Collection("listAggregator");
            _listCol = Db.Collection("list");
            _listItemCol = Db.Collection("listItem");
            _invitationsCol = Db.Collection("invitations");
            _userListAggrCol = Db.Collection("userListAggregator");
            _usersCol = Db.Collection("users");
            _indexesCol = Db.Collection("indexes");
            _refreshToken = Db.Collection("refreshTokens");
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

            //querrySnapshot = await _userListAggrCol.WhereEqualTo("UserId", userFD.UserId).GetSnapshotAsync();


            //var listUserListAggregatorFD = new List<UserListAggregatorFD>();

            //foreach (var item in querrySnapshot)
            //{
            //    var temp = item.ConvertTo<UserListAggregatorFD>();
            //    listUserListAggregator.Add(temp);
            //}

            var listUserListAggregator = await GetUserListAggrByUserId(userFD.UserId);

            var listUserListAggregatorFD = _mapper.Map<List<UserListAggregatorFD>>(listUserListAggregator);

            return (userDTO, listUserListAggregatorFD);
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

        public async Task<User> GetTreeAsync(string userName)
        {

            var listDTO = new List<ListAggregator>();

            var (user, userAggrList) = await GetUserAggrListAsync(userName);

            var listToRemove = new List<UserListAggregatorFD>();


            foreach (var item in userAggrList)
            {

                var cashed = await _cache.GetAsync<ListAggregator>(item.ListAggregatorId);

                if (cashed != null)
                {
                    cashed.PermissionLevel = item.PermissionLevel;
                    listToRemove.Add(item);
                    listDTO.Add(cashed);
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

        public async Task<bool> IsUserIsListAggregatorAsync(int userId, int listAggregatorId)
        {
            var result = await _cache.GetOrAddAsync(
               Dictionary.UserId + userId
               , () => _userEndpointFD.GetUserListAggrByUserId(userId));

            var data = result.Value.Any(a => a.UserId == userId && a.ListAggregatorId == listAggregatorId);

            //return _userEndpointFD.IsUserHasListAggregatorAsync(userId, listAggregatorId);
            return data;


        }

        public async Task AddUserListAggregationAsync(int userId, int listAggregationId, int permission)
        {

            await _userEndpointFD.AddUserListAggregationAsync(userId, listAggregationId, permission);

            await _cache.UpdateAsync<List<UserListAggregator>, string>(Dictionary.UserId + userId,
                  userListAggr =>
                  {
                      userListAggr.Add(new UserListAggregator
                      {
                          ListAggregatorId = listAggregationId,
                          PermissionLevel = permission,
                          UserId = userId

                      });
                      return Task.FromResult(userListAggr);
                  });
        }

        public Task<bool> IsUserInvitatedToListAggregationAsync(int userId, int listAggregationId)
        {
            return _userEndpointFD.IsUserInvitatedToListAggregationAsync(userId, listAggregationId);
        }

        public Task<bool> IsUserHasListAggregatorAsync(int userId, int listAggregationId)
        {
            return _userEndpointFD.IsUserHasListAggregatorAsync(userId, listAggregationId);
        }
        public Task<bool> IsUserIsAdminOfListAggregatorAsync(int userId, int listAggregationId)
        {
            return _userEndpointFD.IsUserIsAdminOfListAggregatorAsync(userId, listAggregationId);
        }
        public Task AddInvitationAsync(string toUserName, int listAggregationId, int permission, string fromSenderName)
        {
            return _userEndpointFD.AddInvitationAsync(toUserName, listAggregationId, permission, fromSenderName);
        }

        public Task<List<UserListAggregator>> TryGetTwoAdministratorsOfListAggregationsAsync(int listAggregationId)
        {
            return _userEndpointFD.TryGetTwoAdministratorsOfListAggregationsAsync(listAggregationId);
        }

        public Task<int> GetLastAdminIdAsync(int listAggregationId)
        {
            return _userEndpointFD.GetLastAdminIdAsync(listAggregationId);
        }

        public async Task SetUserPermissionToListAggrAsync(int userId, int listAggregationId, int permission)
        {
            await _userEndpointFD.SetUserPermissionToListAggrAsync(userId, listAggregationId, permission);
            
            await _cache.UpdateAsync<List<UserListAggregator>, string>(Dictionary.UserId + userId,
                userListAggr =>
                {
                    userListAggr.First(a => a.UserId == userId && a.ListAggregatorId == listAggregationId)
                    .PermissionLevel = permission;
                    return Task.FromResult(userListAggr);
                });

            await _cache.UpdateAsync<List<UserPermissionToListAggregation>, string>(Dictionary.UserPermisionListByListAggrID + listAggregationId,

                (listUsersPermToListaggr) =>
                {
                    listUsersPermToListaggr.First(a => a.User.UserId == userId).Permission = permission;

                    return Task.FromResult(listUsersPermToListaggr);
                });
        }

        public async Task DeleteUserListAggrAscync(int userId, int listAggregationId)
        {
            await _userEndpointFD.DeleteUserListAggrAscync(userId, listAggregationId);

            await _cache.UpdateAsync<List<UserListAggregator>, string>(Dictionary.UserId + userId,
               userListAggr =>
               {
                   userListAggr.Remove(
                    userListAggr.First(a => a.UserId == userId && a.ListAggregatorId == listAggregationId));

                   return Task.FromResult(userListAggr);
               });

            await _cache.UpdateAsync<List<UserPermissionToListAggregation>, string>(Dictionary.UserPermisionListByListAggrID + listAggregationId,

                 (listUsersPermToListaggr) =>
                 {
                     listUsersPermToListaggr.Remove(listUsersPermToListaggr.First(a => a.User.UserId == userId));

                     return Task.FromResult(listUsersPermToListaggr);
                 });
        }


        public async Task<List<ListAggregationWithUsersPermission>> GetListAggregationForPermissionAsync_cached(string userName)
        {

            var lists = await GetListAggrWithUsersPerm_EmptyAsync(userName);

            foreach (var list in lists)
            {

                list.UsersPermToListAggr = await GetListUsersPermissionByListAggrIdAsync(list.ListAggregator.ListAggregatorId);
            }


            return lists;
        }


        public Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPermAsync(string userName)
        {

            return GetListAggregationForPermissionAsync_cached(userName);

            //return _userEndpointFD.GetListAggregationForPermissionAsync(userName);
        }

        public Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPerm2Async(string userName)
        {
            return GetListAggregationForPermissionAsync_cached(userName);

            //return _userEndpointFD.GetListAggregationForPermission2Async(userName);
        }

        public async Task<List<UserListAggregator>> GetUserListAggrByUserId(int userId)
        {



            var cashed = await _cache.GetAsync<List<UserListAggregator>>(Dictionary.UserId + userId);

            if (cashed != null)
            {
                return cashed;
            }

            var data = await _userEndpointFD.GetUserListAggrByUserId(userId);

            await _cache.SetAsync(Dictionary.UserId + userId, data);

            return data;
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

        public Task<List<int>> GetUserIdsFromListAggrIdAsync(int listAggregationId)
        {
            return _userEndpointFD.GetUserIdsFromListAggrIdAsync(listAggregationId);
        }

        public async Task<User> GetUserById(int userId)
        {
            var querrySnapshot = await _usersCol.WhereEqualTo(nameof(UserFD.UserId), userId).GetSnapshotAsync();

            var documentSnapshot = querrySnapshot.FirstOrDefault();
            var userFD = documentSnapshot.ConvertTo<UserFD>();
            userFD.Id = documentSnapshot.Id;

            return _mapper.Map<User>(userFD);
        }

        async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPerm_EmptyAsync(string userName)
        {
            var userTree = await GetTreeAsync(userName);


            return userTree.ListAggregators.Where(a => a.PermissionLevel == 1).Select(a =>
            {
                a.Lists = null;
                return new ListAggregationWithUsersPermission
                {
                    ListAggregator = a

                };
            }).ToList();

            // return _userEndpointFD.GetListAggregationForPermission_EmptyAsync(userId);

        }
        public async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPerm_EmptyAsync(int userId)
        {
            var user = await GetUserById(userId);

            return await GetListAggrWithUsersPerm_EmptyAsync(user.EmailAddress);
        }

        public async Task<List<UserPermissionToListAggregation>> GetListUsersPermissionByListAggrIdAsync(int listAggregatorId)
        {
            //var cashed = await _cache
            //    .GetAsync<ListAggregationForPermission>("ListAggregationForPermission_" + listAggregationForPermission.ListAggregatorEntity.ListAggregatorId);

            //if (cashed != null)
            //{
            //    return cashed;
            //}

            //var data = await _userEndpointFD.GetListAggregationForPermissionByListAggrIdAsync(listAggregationForPermission);

            //await _cache.SetAsync("ListAggregationForPermission_" + listAggregationForPermission.ListAggregatorEntity.ListAggregatorId, data);

            var result = await _cache.GetOrAddAsync(
                Dictionary.UserPermisionListByListAggrID + listAggregatorId
                , () => _userEndpointFD.GetListUsersPermissionByListAggrIdAsync(listAggregatorId));

            var data = result.Value;
            return data;

        }

        public Task AddRefreshToken(int userId, RefreshTokenSession refreshTokenSession)
        {
            return _userEndpointFD.AddRefreshToken(userId, refreshTokenSession);

        }


        public  Task<List<RefreshTokenSession>> GetRefreshTokens(int userId)
        {
            return _userEndpointFD.GetRefreshTokens(userId);
        }
        public  Task DeleteRefreshToken(int userId, RefreshTokenSession refreshTokenSession)
        {
           return _userEndpointFD.DeleteRefreshToken( userId, refreshTokenSession);

        }
        public  Task ReplaceRefreshToken(int userId, RefreshTokenSession oldRefreshTokenSession, RefreshTokenSession newRefreshTokenSession)
        {
          return _userEndpointFD.ReplaceRefreshToken( userId, oldRefreshTokenSession, newRefreshTokenSession);

        }

        public  Task<(string, string)> ReplaceRefreshToken2(int userId, string deviceId, string refreshTokenOld, string accessTokenNew,
            string jti, int version, string refreshTokenNew, CancellationToken cancellationToken)
        {
           return _userEndpointFD.ReplaceRefreshToken2(userId, deviceId, refreshTokenOld, accessTokenNew, jti, version, refreshTokenNew, cancellationToken);
        }
        public  Task DeleteRefreshTokenByJti(int userId, string jti)
        {
            return _userEndpointFD.DeleteRefreshTokenByJti(userId, jti);
         
        }
      
       
    }



}
