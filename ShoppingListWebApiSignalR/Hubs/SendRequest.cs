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


        public async Task SendAsyncAllTree(IEnumerable<int> list)
        {
            //await Clients.All.SendAsync("DataAreChanged");

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            foreach (var item in list)
            {

                    tasks[i++] =  Clients.Others.SendAsync("DataAreChanged_"+item);

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

        public async Task SendAsyncListItem(IEnumerable<int> list, string command, int? id1, int? listAggregationId, int? parentId)
        {
            //await Clients.All.SendAsync("DataAreChanged");

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            foreach (var item in list)
            {
                var a = Clients.Others;
                var b = Clients.All;

                tasks[i++] = Clients.Others.SendAsync("ListItemAreChanged_" + item, command, id1, listAggregationId, parentId);

            }

            await Task.WhenAll(tasks);
        }


    }
}
