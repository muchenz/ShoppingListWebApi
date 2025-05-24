using EFDataBase;
using MediatR;
using Microsoft.AspNetCore.Http;
using ServiceMediatR.SignalREvents;
using Shared.DataEndpoints.Abstaractions;
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
        private readonly IMediator _mediator;
        private readonly ShopingListDBContext _context;

        public MiddlewareSignalR(RequestDelegate next, IMediator mediator)
        {
            _next = next;
            _mediator = mediator;
        }
        public async Task InvokeAsync(HttpContext context, IUserEndpoint userEndpoint)
        {
            // Call the next delegate/middleware in the pipeline
            await _next(context);

            var respons = context.Response;

            if (respons.Headers.ContainsKey("command"))
            {

                var command = respons.Headers["command"];
                int id1 = int.Parse(respons.Headers["id1"]);
                int id2 = int.Parse(respons.Headers["id2"]);

                await _mediator.Publish(new ListItemEditedSignalRNotification( id1, id2, null));

            }
        }
    }
}