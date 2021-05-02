using ShoppingListWebApi.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Handlers
{
    public class RespondSignalRHandler : DelegatingHandler
    {
        //private readonly ShopingListDBContext _context;
        //private readonly SignarRService _signarRService;

        //public RespondSignalRHandler(ShopingListDBContext context, SignarRService signarRService)
        //{
        //    _context = context;
        //    _signarRService = signarRService;
        //}
        public RespondSignalRHandler()
        {

        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            var ii="";
            var respons = await base.SendAsync(request, cancellationToken);


            //if (respons.Headers.Contains("command"))
            //{

            //    var command = respons.Headers.GetValues("command").First();
            //    int  id1 = int.Parse( respons.Headers.GetValues("id1").First());
            //    int  id2 = int.Parse(respons.Headers.GetValues("id2").First());

            //     var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(id2, _context);
            //    await _signarRService.SendRefreshMessageToUsersAsync(userList, "Edit/Save_ListItem", id1, id2);

            //}


            return respons;

        }
    }
}
