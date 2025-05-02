using AutoMapper;
using EFDataBase;
using Google.Api;
using Google.Cloud.Firestore;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Google.Cloud.Firestore.V1.StructuredQuery.Types;

namespace FirebaseDatabase
{
    public class FirebaseDatabase
    {

        public static FirestoreDb Db;
        static Mapper _mapper;
        public static void ConnectToFirebase()
        {


            var config = new MapperConfiguration(cfg => cfg.CreateMap<Order, Order>());

            _mapper = new Mapper(config);

           
            string filepath = @"e:\testnosqldb1-firebase-adminsdk-c123k-89b708d87e.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);

            Db = FirestoreDb.Create("testnosqldb1");

        }

        public static async Task SetUser(List<UserEntity> usersDB)
        {

            ConnectToFirebase();


            CollectionReference colection = Db.Collection("users");


            foreach (var user in usersDB)
            {


                var userFD = new UserFD { EmailAddress = user.EmailAddress, LoginType = user.LoginType,
                    Password = user.Password, UserId=user.UserId };

                user.UserRoles.ToList().ForEach(a => userFD.Roles.Add(a.Role.RoleName));


                var docRef = colection.Document(user.UserId.ToString());

                await docRef.SetAsync(userFD);
            }
        }
        public static async Task SetAllList(List<ListAggregatorEntity> listDB)
        {


            ConnectToFirebase();


            CollectionReference listAggrCol = Db.Collection("listAggregator");
           CollectionReference listCol = Db.Collection("list");
            CollectionReference listItemCol = Db.Collection("listItem");

            foreach (var listAggr in listDB)
            {


                var listAggrFD = new ListAggregatorFD { ListAggregatorName = listAggr.ListAggregatorName, 
                    Order = listAggr.Order, ListAggregatorId=listAggr.ListAggregatorId };

                listAggr.Lists.ToList().ForEach(a => listAggrFD.Lists.Add(a.ListId));


                var docRefListAggrFD = listAggrCol.Document(listAggr.ListAggregatorId.ToString());

                await docRefListAggrFD.SetAsync(listAggrFD);


                foreach (var list in listAggr.Lists)
                {


                    var listFD = new ListFD { ListName = list.ListName, Order = list.Order, 
                        ListAggrId=listAggr.ListAggregatorId, ListId=list.ListId };

                    list.ListItems.ToList().ForEach(a => listFD.ListItems.Add(a.ListItemId));


                    var List = listCol.Document(list.ListId.ToString());

                    await List.SetAsync(listFD);




                    foreach (var listItem in list.ListItems)
                    {


                        var listItemFD = new ListItemFD { ListItemName = listItem.ListItemName, Order = listItem.Order,
                            State = listItem.State, ListAggrId=listAggr.ListAggregatorId, ListItemId=listItem.ListItemId,
                        ListId=list.ListId};



                        var ListItem = listItemCol.Document(listItem.ListItemId.ToString());

                        await ListItem.SetAsync(listItemFD);
                    }
                }


            }
        }
        public static async Task SetListAggregator(List<ListAggregatorEntity> listDB)
        {


            ConnectToFirebase();


            CollectionReference colection = Db.Collection("listAggregator");


            foreach (var item in listDB)
            {


                var itemFD = new ListAggregatorFD { ListAggregatorName = item.ListAggregatorName, Order = item.Order
                    ,  ListAggregatorId=item.ListAggregatorId};

                item.Lists.ToList().ForEach(a => itemFD.Lists.Add(a.ListId));


                var docRef = colection.Document(item.ListAggregatorId.ToString());

                await docRef.SetAsync(itemFD);
            }
        }
        public static async Task SetList(List<ListEntity> listDB)
        {


            ConnectToFirebase();


            CollectionReference colection = Db.Collection("list");


            foreach (var item in listDB)
            {


                var itemFD = new ListFD { ListName = item.ListName, Order = item.Order, ListId=item.ListId };

                item.ListItems.ToList().ForEach(a => itemFD.ListItems.Add(a.ListItemId));


                var docRef = colection.Document(item.ListId.ToString());

                await docRef.SetAsync(itemFD);
            }
        }

        public static async Task SetListItem(List<ListItemEntity> listDB)
        {


            ConnectToFirebase();


            CollectionReference colection = Db.Collection("listItem");


            foreach (var item in listDB)
            {


                var itemFD = new ListItemFD { ListItemName = item.ListItemName, Order = item.Order, State = item.State };



                var docRef = colection.Document(item.ListItemId.ToString());

                await docRef.SetAsync(itemFD);
            }
        }

        public static async Task SetUserListAggregator(List<UserListAggregatorEntity> listDB)
        {


            ConnectToFirebase();


            CollectionReference colection = Db.Collection("userListAggregator");


            foreach (var item in listDB)
            {


                var itemFD = new UserListAggregatorFD
                {
                    ListAggregatorId = item.ListAggregatorId,
                    UserId = item.UserId,
                    PermissionLevel = item.PermissionLevel,
                    State = item.State

                };



                //var docRef = colection.Document(item..ToString());

                await colection.AddAsync(itemFD);
            }
        }

        public static async Task<User> GetTree(string userName)
        {


            ConnectToFirebase();


            CollectionReference userListAggregatorColection = Db.Collection("userListAggregator");
            CollectionReference listItemColection = Db.Collection("listItem");
            CollectionReference listColection = Db.Collection("list");
            CollectionReference listAggregatorColection = Db.Collection("listAggregator");
            CollectionReference usersColection = Db.Collection("users");


            var aa = new ListAggregatorFD() {  };


            var querrySnapshot = await usersColection.WhereEqualTo("EmailAddress", userName).GetSnapshotAsync();

            var documentSnapshot  = querrySnapshot.FirstOrDefault();
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

            querrySnapshot = await userListAggregatorColection.WhereEqualTo("UserId", userFD.Id).GetSnapshotAsync();


            var listUserListAggregator = new List<UserListAggregatorFD>();

            foreach (var item in querrySnapshot)
            {
                var temp = item.ConvertTo<UserListAggregatorFD>();
                listUserListAggregator.Add(temp);
            }


            var listListAggregatortDTO = new List<ListAggregator>();

            foreach (var userListAggregator in listUserListAggregator)
            {
                var docSnapListAggr  = await listAggregatorColection
                    .Document(userListAggregator.ListAggregatorId.ToString()).GetSnapshotAsync();

                var tempListAggrFD = docSnapListAggr.ConvertTo<ListAggregatorFD>();

                var tempListAggrDTO = new ListAggregator
                {
                    ListAggregatorId = int.Parse(docSnapListAggr.Id),
                    ListAggregatorName = tempListAggrFD.ListAggregatorName
                ,
                    PermissionLevel = userListAggregator.PermissionLevel
                };

                listListAggregatortDTO.Add(tempListAggrDTO);


                foreach (var listId in tempListAggrFD.Lists)
                {
                    var docSnapList = await listColection.Document(listId.ToString()).GetSnapshotAsync();

                    var tempListFD = docSnapList.ConvertTo<ListFD>();

                    var tempListDTO = new List
                    {
                        ListId = int.Parse(docSnapList.Id),
                        ListName = tempListFD.ListName, 
                        Order= tempListFD.Order

                    };

                    tempListAggrDTO.Lists.Add(tempListDTO);


                    foreach (var listItemId in tempListFD.ListItems)
                    {
                        var docSnapListItem = await listItemColection.Document(listItemId.ToString()).GetSnapshotAsync();

                        var tempListItemFD = docSnapListItem.ConvertTo<ListItemFD>();

                        var tempListItemDTO = new ListItem
                        {
                            ListItemId = int.Parse(docSnapListItem.Id),
                            ListItemName = tempListItemFD.ListItemName,
                            Order = tempListItemFD.Order,
                            State = tempListItemFD.State

                        };

                        tempListDTO.ListItems.Add(tempListItemDTO);
                    }


                }



            }
            userDTO.ListAggregators = listListAggregatortDTO;


            return userDTO;

            //userQerrySnap.i


        }



        public static async Task DeleteTestMethod()
        {

            ConnectToFirebase();


            CollectionReference colection = Db.Collection("userListAggregator");

            var querrySnapshot = await colection.WhereEqualTo(nameof(UserListAggregatorFD.UserId), "99")
                .WhereEqualTo(nameof(UserListAggregatorFD.ListAggregatorId), "99").GetSnapshotAsync();

            var result = await querrySnapshot.FirstOrDefault().Reference.DeleteAsync();

        }

    }
}
