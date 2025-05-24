using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceMediatR.ListItemCommandAndQueries;
using ServiceMediatR.SignalREvents;
using ServiceMediatR.UserCommandAndQuerry;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using ShoppingListWebApi.Auth.Api;
using ShoppingListWebApi.Data;


namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListItemController : ControllerBase
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly IListItemEndpoint _listItemEndpoint;
        private readonly IUserEndpoint _userEndpoint;
        private readonly IConfiguration _configuration;

        public ListItemController(IConfiguration configuration, IMapper mapper,
             IMediator mediator, IListItemEndpoint listItemEndpoint, IUserEndpoint userEndpoint
           )//, IConfiguration configuration)
        {
            _mapper = mapper;
            _mediator = mediator;
            _listItemEndpoint = listItemEndpoint;
            _userEndpoint = userEndpoint;
            _configuration = configuration;

        }


        [HttpPost("AddListItem")]
        [SecurityLevel(2)]

        public async Task<ActionResult<ListItem>> AddListItemToListt(int parentId, [FromBody] ListItem item
            , int listAggregationId, [FromHeader] string signalRId)
        {

            if (!await CheckIntegrityListAsync(parentId, listAggregationId)) return Forbid();


            var listItem = await _listItemEndpoint.AddListItemAsync(parentId, item, listAggregationId);

            item.ListItemId = listItem.ListItemId;



            await _mediator.Publish(
                new ListItemAddedSignalRNotification(listItem.ListItemId, listAggregationId
                , parentId, signalRId));


            return await Task.FromResult(item);
        }


        [HttpPost("DeleteListItem")]
        [SecurityLevel(1)]
        public async Task<ActionResult<int>> DeleteListItem(int ItemId, int listAggregationId, [FromHeader] string signalRId)
        {
            if (!await CheckIntegrityListItemAsync(ItemId, listAggregationId)) return Forbid();

            var amount = await _listItemEndpoint.DeleteListItemAsync(ItemId, listAggregationId);


            await _mediator.Publish(new ListItemDeletedSignalRNotification(ItemId, listAggregationId, signalRId));


            return await Task.FromResult(amount);
        }

        [HttpPost("GetItemListItem")]
        [SecurityLevel(3)]
        public async Task<ActionResult<ListItem>> GetItemListItem(int ItemId, int listAggregationId)
        {
            if (!await CheckIntegrityListItemAsync(ItemId, listAggregationId)) return Forbid();

            var listItem = await _listItemEndpoint.GetItemListItemAsync(ItemId);

            return await Task.FromResult(listItem);
        }

        async Task<bool> CheckIntegrityListItemAsync(int listItemId, int listAggregationId)
        {

            return await _listItemEndpoint.CheckIntegrityListItemAsync(listItemId, listAggregationId);
        }

        async Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId)
        {

            return await _listItemEndpoint.CheckIntegrityListAsync(listId, listAggregationId);


        }


        [HttpPost("EditListItem")]
        //[Authorize]
        [SecurityLevel(2)]
        public async Task<ActionResult<ListItem>> EditListItem([FromBody] ListItem item, int listAggregationId
            , [FromHeader] string signalRId)
        {

            if (!await CheckIntegrityListItemAsync(item.ListItemId, listAggregationId)) return Forbid();


            var listItem = await _listItemEndpoint.EditListItemAsync(item, listAggregationId);

            await _mediator.Publish(new ListItemEditedSignalRNotification(listItem.ListItemId
                , listAggregationId, signalRId));

            return await Task.FromResult(listItem);
        }

        [HttpPost("ChangeOrderListItem")]
        [Authorize]
        public async Task<ActionResult<bool>> ChangeOrderListItem([FromBody] IEnumerable<ListItem> items)
        {

            await _listItemEndpoint.ChangeOrderListItemAsync(items);

            return await Task.FromResult(true);
        }

        [HttpPost("SaveProperty")]
        //[Authorize]
        [SecurityLevel(3)]
        public async Task<ActionResult<ListItem>> SaveProperty([FromBody] ListItem item, string propertyName
            , int listAggregationId, [FromHeader] string signalRId)
        {
            /*

            Stopwatch sw = new Stopwatch();

            sw.Start();

            var t1 = sw.ElapsedMilliseconds;
            Debug.WriteLine(t1);
            if (!CheckIntegrityListItem(item.ListItemId, listAggregationId)) return Forbid();

            Debug.WriteLine(sw.ElapsedMilliseconds - t1);
            var listItemEntity = _mapper.Map<ListItemEntity>(item);

            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));
           

            _context.Entry<ListItemEntity>(listItemEntity).Property(propertyName).IsModified = true;
            var amount = await _context.SaveChangesAsync();
            Debug.WriteLine(sw.ElapsedMilliseconds - t1);
            var listItem = _mapper.Map<ListItem>(listItemEntity);

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            Debug.WriteLine(sw.ElapsedMilliseconds - t1);
            await _signarRService.SendRefreshMessageToUsersAsync(userList, "Edit/Save_ListItem", listItem.ListItemId, listAggregationId);
            Debug.WriteLine(sw.ElapsedMilliseconds - t1);

            //ControllerContext.HttpContext.Response.Headers.Add("command", "Edit/Save_ListItem");
            //ControllerContext.HttpContext.Response.Headers.Add("id1", listItem.ListItemId.ToString());
            //ControllerContext.HttpContext.Response.Headers.Add("id2", listAggregationId.ToString());
            */

            /////////////////////////////////////

            Stopwatch sw = new Stopwatch();

            sw.Start();
            var t1 = sw.ElapsedMilliseconds;
            var res = await _mediator.Send(new SavePropertyCommand(item, propertyName, listAggregationId));

            Debug.WriteLine(sw.ElapsedMilliseconds - t1);

            // await _signarRService.SendRefreshMessageToUsersAsync(users.Data, "Edit/Save_ListItem", item.ListItemId, listAggregationId);

            await _mediator.Publish(new ListItemEditedSignalRNotification(item.ListItemId, listAggregationId,signalRId));


            return await Task.FromResult(res.Data);
        }
    }
}