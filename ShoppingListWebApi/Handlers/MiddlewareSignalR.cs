using EFDataBase;
using Microsoft.AspNetCore.Http;
using ShoppingListWebApi.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Handlers
{


    public class MiddlewareSignalR
    {
        private readonly RequestDelegate _next;
        private readonly ShopingListDBContext _context;
        private readonly SignarRService _signarRService;

        public MiddlewareSignalR(RequestDelegate next, SignarRService signarRService)
        {
            _next = next;
          
            _signarRService = signarRService;
        }


        public async Task InvokeAsync(HttpContext context, ShopingListDBContext _context)
        {


            // Call the next delegate/middleware in the pipeline
            await _next(context);

            var respons = context.Response;

            if (respons.Headers.ContainsKey("command"))
            {

                var command = respons.Headers["command"];
                int id1 = int.Parse(respons.Headers["id1"]);
                int id2 = int.Parse(respons.Headers["id2"]);

                var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(id2, _context);
                await _signarRService.SendRefreshMessageToUsersAsync(userList, "Edit/Save_ListItem", id1, id2);

            }
        }
    }
}