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


        public async Task SendToAsync(IEnumerable<int> list, string command, int? id1, int? listAggregationId, int? parentId)
        {
            //await Clients.All.SendAsync("DataAreChanged");

            Task[] tasks = new Task[list.Count()];
            int i = 0;

            foreach (var item in list)
            {

                    tasks[i++] =  Clients.Others.SendAsync("DataAreChanged_"+item, command, id1, listAggregationId, parentId);

            }

            await Task.WhenAll(tasks);
        }




    }
}
