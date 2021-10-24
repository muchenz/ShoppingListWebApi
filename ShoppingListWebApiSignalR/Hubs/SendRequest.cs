using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingListWebApiSignalR.Hubs
{
    public class SendRequest:Hub
    {
         
        public SendRequest()
        {

            //Task.Run(async () => {

            //    while (true)
            //    {
            //        await Task.Delay(1000);

            //        await Clients.Others.SendAsync("DataAreChanged");
            //    }
            
            //});


        }


        public async Task SendAsyncAllTree(IEnumerable<int> list, string signalRId)
        {
            //await Clients.All.SendAsync("DataAreChanged");

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            foreach (var item in list)
            {

                    tasks[i++] =  Clients.AllExcept("").SendAsync("DataAreChanged_"+item);

            }

            await Task.WhenAll(tasks);
        }

        public async Task SendAsyncNewIvitation(IEnumerable<int> list)
        {
            //await Clients.All.SendAsync("DataAreChanged");

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            foreach (var item in list)
            {

                tasks[i++] = Clients.Others.SendAsync("NewInvitation_" + item);

            }

            await Task.WhenAll(tasks);
        }

        public async Task SendAsyncListItem(IEnumerable<int> list, string command, int? id1, int? listAggregationId
            , int? parentId, string signalRId)
        {
            //await Clients.All.SendAsync("DataAreChanged");

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            //var listId = ConnectedUser.Ids.Where(a => a != signalRId && a != Context.ConnectionId).ToList();

            foreach (var item in list)
            {
                var a = Clients.Others;
                var b = Clients.All;

                tasks[i++] = Clients.AllExcept(signalRId).SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);

            }

            await Task.WhenAll(tasks);
        }

        //public override Task OnConnectedAsync()
        //{
        //    ConnectedUser.Ids.Add(Context.ConnectionId);
        //    return base.OnConnectedAsync();
        //}
        
        //public override Task OnDisconnectedAsync(Exception exception)
        //{
        //    ConnectedUser.Ids.Remove(Context.ConnectionId);
        //    return base.OnDisconnectedAsync(exception);
        //}
        //public static class ConnectedUser
        //{
        //    public static List<string> Ids = new List<string>();
        //}
    }
}
