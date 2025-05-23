using FirebaseDatabase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ShoppingListWebApi.Hub.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Hub
{
    [Authorize(AuthenticationSchemes = CustomSchemeHandler.CustomScheme)]
    //[Authorize]
    public class SendRequest : Microsoft.AspNetCore.SignalR.Hub
    {


        public async Task SendAsyncAllTree(IEnumerable<int> list, string signalRId)
        {

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            foreach (var item in list)
            {

                //tasks[i++] = Clients.AllExcept(signalRId).SendAsync("DataAreChanged_" + item);
                tasks[i++] = Clients.All.SendAsync("DataAreChanged_" + item);
                //tasks[i++] =  Clients.User(item.ToString()).SendAsync("DataAreChanged_"+item); only to user with concret ID

            }

            await Task.WhenAll(tasks);
        }

        public async Task SendAsyncIvitationChanged(IEnumerable<int> list, string signalRId)
        {

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            foreach (var item in list)
            {

                //tasks[i++] = Clients.AllExcept(signalRId).SendAsync("NewInvitation_" + item);
                tasks[i++] = Clients.All.SendAsync("InvitationAreChanged_" + item);
                //tasks[i++] = Clients.User(item.ToString()).SendAsync("NewInvitation_" + item);

            }

            await Task.WhenAll(tasks);
        }

        //[Authorize(policy:"HUB")]
        //public async Task SendAsyncListItem(IEnumerable<int> list, string command, int? id1, int? listAggregationId
        //    , int? parentId, string signalRId)
        //{
        //    var userID = this.Context.UserIdentifier;

        //    //var sinalRIdList = ConnectedUser.GetIdentifiers(list.ToList(), signalRId);


        //    Task[] tasks = new Task[list.Count()];
        //    int i = 0;

        //    //var listId = ConnectedUser.Ids.Where(a => a != signalRId && a != Context.ConnectionId).ToList();

        //    foreach (var item in list)
        //    {
        //        var a = Clients.Others;
        //        var b = Clients.All;

        //        //tasks[i++] = Clients.Clients(sinalRIdList).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);
        //        //tasks[i++] = Clients.AllExcept(signalRId).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);
        //        tasks[i++] = Clients.Users(list.Select(a=>a.ToString())).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);
        //        //tasks[i++] = Clients.User(item.ToString()).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);

        //    }

        //    await Task.WhenAll(tasks);
        //}


        public async Task SendAsyncListItem(IEnumerable<int> list, string eventName, object signalREvent)
        {
            var userID = this.Context.UserIdentifier;

            //var sinalRIdList = ConnectedUser.GetIdentifiers(list.ToList(), signalRId);


            Task[] tasks = new Task[list.Count()];
            int i = 0;

            //var listId = ConnectedUser.Ids.Where(a => a != signalRId && a != Context.ConnectionId).ToList();

            foreach (var item in list)
            {
                var a = Clients.Others;
                var b = Clients.All;

                //tasks[i++] = Clients.Clients(sinalRIdList).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);
                //tasks[i++] = Clients.AllExcept(signalRId).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);
                tasks[i++] = Clients.Users(list.Select(a => a.ToString())).SendAsync("ListItemAreChanged_"+item, signalREvent);
                //tasks[i++] = Clients.User(item.ToString()).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);

            }

            await Task.WhenAll(tasks);
        }

        //----------------------------------
        public override Task OnConnectedAsync()
        {
            var userID = this.Context.UserIdentifier;

            //ConnectedUser.Add(userID, Context.ConnectionId);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userID = this.Context.UserIdentifier;

            //ConnectedUser.Remove(userID, Context.ConnectionId);

            return base.OnDisconnectedAsync(exception);
        }




        public static class ConnectedUser
        {
            // to do safe thead list

            // <ConnectionId, UserIdentifier >
            public static Dictionary<string, List<string>> Dictionary = new Dictionary<string, List<string>>();

            public static void Add(string userId, string connectionID)
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionID))  return; 

                lock (Dictionary)
                {
                    if (Dictionary.TryGetValue(userId, out var list))
                    {
                        list.Add(connectionID);
                        Dictionary[userId] = list;
                        return;
                    }

                    Dictionary[userId] = new List<string>() { connectionID };
                }
            }
            public static void Remove(string userId, string connectionID)
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionID)) return;

                lock (Dictionary)
                {

                    if (Dictionary.TryGetValue(userId, out var list))
                    {
                        var index = list.IndexOf(connectionID);

                        if (index == -1) return;
                        list.RemoveAt(index);

                        Dictionary[userId] = list;
                        return;
                    }
                }

            }

            public static List<string> GetIdentifiers(List<int> usersId, string connectionIDToExclude)
            {
               
                //--------------
                //var result = usersId.Select(a => a.ToString()).Aggregate(
                //        new List<string>(),
                //        (acc, userId) =>
                //        {
                //            if (Dictionary.TryGetValue(userId, out var connections))
                //                acc.AddRange(connections);
                //            return acc;
                //        });

                //-----------------

                var ids = usersId.Select(a => a.ToString())
                    .Where(Dictionary.ContainsKey)
                    .SelectMany(id => Dictionary[id])
                    .ToList();

                ids.Remove(connectionIDToExclude);

                return ids;
            }

        }
    }
}
