using AutoMapper;
using Google.Cloud.Firestore;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseDatabase
{
    public class UserEndpointFD : IUserEndpoint
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

        public UserEndpointFD(IMapper mapper)
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

        public async Task AddInvitationAsync(string toUserName, int listAggregationId, int permission, string fromSenderName)
        {
            var invitationFD = new InvitationFD
            {
                EmailAddress = toUserName,
                ListAggregatorId = listAggregationId,
                PermissionLevel = permission,
                SenderName = fromSenderName
            };

            await Db.RunTransactionAsync(async transation =>
            {


                var docRef = transation.Database.Collection("indexes").Document("indexes");
                var snapDoc = await docRef.GetSnapshotAsync();

                var index = snapDoc.GetValue<long>("invitations");

                var newDoc = transation.Database.Collection("invitations").Document((index + 1).ToString());
                invitationFD.InvitationId = (int)index + 1;
                await newDoc.SetAsync(invitationFD);

                await docRef.UpdateAsync("invitations", index + 1);


            });

        }

        public async Task AddUserListAggregationAsync(int userId, int listAggregationId, int permission)
        {
            var userListAggregatorFD = new UserListAggregatorFD
            {
                UserId = userId,
                ListAggregatorId = listAggregationId,
                PermissionLevel = permission
            };


            await _userListAggrCol.AddAsync(userListAggregatorFD);
        }

        public async Task DeleteUserListAggrAscync(int userId, int listAggregationId)
        {

            var querrySnapshot = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
                .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId).GetSnapshotAsync();

            await querrySnapshot.FirstOrDefault().Reference.DeleteAsync();
        }

        public async Task<User> FindUserByIdAsync(int id)
        {

            var refDoc = await _usersCol.Document(id.ToString()).GetSnapshotAsync();
            if (!refDoc.Exists) return null;
            var userFD = refDoc.ConvertTo<UserFD>();
            userFD.Id = id.ToString();
            var user = _mapper.Map<User>(userFD);
            return user;
        }

        public async Task<List<User>> FindUsersByListOfIdAsync(IEnumerable<int> listUserId)
        {

            if (!listUserId.Any()) return null;

            var docUsersSnapshotsList = await RestrictQuerryListTo10Async(
                      listUserId,
                      (i, list) =>
                      _usersCol.WhereIn(nameof(UserFD.UserId),
                           list.Skip(10 * i).Take(10)).GetSnapshotAsync()
                      );


            //var sanap = await _usersCol.WhereIn(nameof(UserFD.UserId), listUserId).GetSnapshotAsync();

            if (!docUsersSnapshotsList.Any()) return null;

            var listUser = docUsersSnapshotsList.Select(a =>
            {
                var us = _mapper.Map<User>(a.ConvertTo<UserFD>());

                return us;
            }).ToList();

            return listUser;
        }

        public async Task<int> GetLastAdminIdAsync(int listAggregationId)
        {

            var querrySnapshot =
                await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId)
               .WhereEqualTo(nameof(UserListAggregatorFD.PermissionLevel), 1).GetSnapshotAsync();

            return querrySnapshot.FirstOrDefault().ConvertTo<UserListAggregatorFD>().UserId;
        }

        public Task<List<ListAggregationForPermission>> GetListAggregationForPermission(string userName)
        {
            return GetListAggregationForPermission2(userName);
        }

        public async Task<List<ListAggregationForPermission>> GetListAggregationForPermission2(string userName)
        {

            ///
            var snapUserFD = await _usersCol.WhereEqualTo(nameof(UserFD.EmailAddress), userName).GetSnapshotAsync();

            if (snapUserFD.Count == 0) return new List<ListAggregationForPermission>();

            var userId = int.Parse(snapUserFD.First().Id);

            var userListAggrSnap = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
                .WhereEqualTo(nameof(UserListAggregatorFD.PermissionLevel), 1).GetSnapshotAsync();

            var userListAggregatorsFD = userListAggrSnap.Documents.Select(a => a.ConvertTo<UserListAggregatorFD>()).ToList();


            var listAggregators = new List<ListAggregator>();


             if (!userListAggregatorsFD.Any()) return new List<ListAggregationForPermission>(); 

            //var listAggrDocSanp =  await  _listAggrCol.WhereIn(nameof(ListAggregatorFD.ListAggregatorId), 
            //                              userListAggregatorsFD.Select(a=>a.ListAggregatorId)).GetSnapshotAsync();


            var listAggrDocSanp = await RestrictQuerryListTo10Async(
                    userListAggregatorsFD.Select(a => a.ListAggregatorId),
                    (i, list) =>
                    _listAggrCol.WhereIn(nameof(ListAggregatorFD.ListAggregatorId),
                         list.Skip(10 * i).Take(10)).GetSnapshotAsync()
                    );



            //foreach (var item in userListAggregatorsFD)
            foreach (var item in listAggrDocSanp)
            {
                //var itemListAggrSnap = await _listAggrCol.Document(item.ListAggregatorId.ToString()).GetSnapshotAsync();

                var tempListAggrFD = item.ConvertTo<ListAggregatorFD>();

                var listAggrItemTemp = _mapper.Map<ListAggregator>(tempListAggrFD);

                listAggrItemTemp.PermissionLevel = userListAggregatorsFD.Where(a => a.ListAggregatorId == tempListAggrFD.ListAggregatorId)
                    .FirstOrDefault().PermissionLevel;

                listAggregators.Add(listAggrItemTemp);
            }




            var dataTransfer = new List<ListAggregationForPermission>();


            var userListAggrDocSanp = await RestrictQuerryListTo10Async(
                   listAggregators.Select(a => a.ListAggregatorId),
                   (i, list) =>
                   _userListAggrCol.WhereIn(nameof(UserListAggregatorFD.ListAggregatorId),
                        list.Skip(10 * i).Take(10)).GetSnapshotAsync()
                   );

            var userListAggr = userListAggrDocSanp.Select(a => a.ConvertTo<UserListAggregatorFD>());

            //var userListAggr = (await _userListAggrCol.WhereIn(nameof(UserListAggregatorFD.ListAggregatorId), 
            //                                            listAggregators.Select(a=>a.ListAggregatorId))
            //                                                .GetSnapshotAsync()).Select(a=>a.ConvertTo<UserListAggregatorFD>());

            foreach (var listAggr in listAggregators)
            {
                var tempListAggregationForPermission = new ListAggregationForPermission();

                dataTransfer.Add(tempListAggregationForPermission);

                tempListAggregationForPermission.ListAggregatorEntity = listAggr;


                tempListAggregationForPermission.Users = new List<UserPermissionToListAggregation>();



                //userListAggrSnap = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggr.ListAggregatorId)
                //                       .GetSnapshotAsync();

                var tempListUserIdAndPermission = userListAggr.Where(a=>a.ListAggregatorId==listAggr.ListAggregatorId).Select(a =>
                {

                  //  var tempUserListAggr = a.ConvertTo<UserListAggregatorFD>();

                    return new
                    {
                        UserId = a.UserId,
                        Permission = a.PermissionLevel
                    };

                }).ToList();
              

                var listUser = await FindUsersByListOfIdAsync(tempListUserIdAndPermission.Select(a => a.UserId));


                foreach (var userR in listUser)
                {
                    // var tempUserFD = await FindUserByIdAsync(item.UserId);

                    //var tempUser = _mapper.Map<User>(userResTask.Result);

                    var tempUser = userR;

                    var tempUserPermissionToListAggregation = new UserPermissionToListAggregation();


                    tempListAggregationForPermission.Users.Add(tempUserPermissionToListAggregation);

                    tempUserPermissionToListAggregation.Permission = tempListUserIdAndPermission
                                                                        .Single(a => a.UserId == userR.UserId).Permission;
                    tempUserPermissionToListAggregation.User = tempUser;

                }

            }

            return dataTransfer;

        }

        public async Task<int> GetNumberOfAdministratorsOfListAggregationsAsync(int listAggregationId)
        {
            var querrySnapshot =
                await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId)
               .WhereEqualTo(nameof(UserListAggregatorFD.PermissionLevel), 1).GetSnapshotAsync();

            return querrySnapshot.Count;

        }

        public async Task<User> GetTreeAsync(string userName)
        {
            CollectionReference userListAggregatorColection = Db.Collection("userListAggregator");
            CollectionReference listItemColection = Db.Collection("listItem");
            CollectionReference listColection = Db.Collection("list");
            CollectionReference listAggregatorColection = Db.Collection("listAggregator");
            CollectionReference usersColection = Db.Collection("users");


            var aa = new ListAggregatorFD() { };


            var querrySnapshot = await usersColection.WhereEqualTo("EmailAddress", userName).GetSnapshotAsync();

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

            querrySnapshot = await userListAggregatorColection.WhereEqualTo("UserId", userFD.UserId).GetSnapshotAsync();


            var listUserListAggregator = new List<UserListAggregatorFD>();

            foreach (var item in querrySnapshot)
            {
                var temp = item.ConvertTo<UserListAggregatorFD>();
                listUserListAggregator.Add(temp);
            }


            var listListAggregatortDTO = new List<ListAggregator>();

           

            if (!listUserListAggregator.Any()) return userDTO;

           

            var docListAggerSnapshotsList = await RestrictQuerryListTo10Async(
                       listUserListAggregator.Select(a => a.ListAggregatorId),
                       (i, list) =>
                       listAggregatorColection.WhereIn(nameof(ListAggregatorFD.ListAggregatorId),
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
                       listColection.WhereIn(nameof(ListFD.ListId),
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
                        listItemColection.WhereIn(nameof(ListItemFD.ListItemId),
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
            userDTO.ListAggregators = listListAggregatortDTO;

            return userDTO;
        }


        async Task<IEnumerable<DocumentSnapshot>> RestrictQuerryListTo10Async(IEnumerable<int> argumentList, Func<int, IEnumerable<int>, Task<QuerySnapshot>> func )
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
        public async Task<User> GetUserByNameAsync(string userName)
        {
            var snapUserFD = await _usersCol.WhereEqualTo(nameof(UserFD.EmailAddress), userName).GetSnapshotAsync();

            if (snapUserFD.Count == 0) return null;

            var userFD = snapUserFD.First().ConvertTo<UserFD>();

            return _mapper.Map<User>(userFD);
        }

        public async Task<List<UserListAggregator>> GetUserListAggrByUserId(int userId)
        {
            var snapUserListAggrFD = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId).GetSnapshotAsync();

            var listUserListAggrFD = snapUserListAggrFD.Documents.Select(a => a.ConvertTo<UserListAggregatorFD>()).ToList();

            var listUserListAggr = _mapper.Map<List<UserListAggregator>>(listUserListAggrFD);

            return listUserListAggr;
        }

        public async Task<List<string>> GetUserRolesByUserIdAsync(int userId)
        {
            return (await FindUserByIdAsync(userId))?.Roles.ToList();
        }

        public Task<User> GetUserWithRolesAsync(int userId)
        {
            return FindUserByIdAsync(userId);
        }

        public async Task<bool> IsUserHasListAggregatorAsync(int userId, int listAggregatorId)
        {
            var userListAggrSnap = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
               .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregatorId).GetSnapshotAsync();

            return userListAggrSnap.Documents.Count > 0;
        }

        public async Task<bool> IsUserInvitatedToListAggregationAsync(string userName, int listAggregationId)
        {
            var userListAggrSnap = await _invitationsCol.WhereEqualTo(nameof(InvitationFD.EmailAddress), userName)
               .WhereEqualTo(nameof(InvitationFD.ListAggregatorId), listAggregationId).GetSnapshotAsync();

            return userListAggrSnap.Documents.Any();
        }

        public async Task<User> LoginAsync(string userName, string password)
        {
            var userFDSnap = await _usersCol.WhereEqualTo(nameof(UserFD.EmailAddress), userName)
               .WhereEqualTo(nameof(UserFD.Password), password).GetSnapshotAsync();

            if (!userFDSnap.Documents.Any()) return null;

            var userFD = userFDSnap.First().ConvertTo<UserFD>();

            return _mapper.Map<User>(userFD);
        }

        public async Task<User> Register(string userName, string password, LoginType loginType)
        {
            var newUserFD = new UserFD
            {
                EmailAddress = userName,
                Password = password,
                LoginType = (byte)loginType,
                Roles = new string[] { "User" }
            };

            await Db.RunTransactionAsync(async transation =>
            {


                var docRef = transation.Database.Collection("indexes").Document("indexes");
                var snapDoc = await docRef.GetSnapshotAsync();

                var index = snapDoc.GetValue<long>("users");

                var newDoc = transation.Database.Collection("users").Document((index + 1).ToString());
                newUserFD.UserId = (int)index + 1;
                await newDoc.SetAsync(newUserFD);

                await docRef.UpdateAsync("users", index + 1);


            });

            return _mapper.Map<User>(newUserFD);
        }

        public async Task SetUserPermissionToListAggrAsync(int userId, int listAggregationId, int permission)
        {
            var userListAggrSnap = await _userListAggrCol.WhereEqualTo(nameof(UserListAggregatorFD.UserId), userId)
               .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), listAggregationId).GetSnapshotAsync();

            var docId = userListAggrSnap.First().Id;

            await _userListAggrCol.Document(docId).UpdateAsync(nameof(UserListAggregatorFD.PermissionLevel), permission);
        }
    }
}
