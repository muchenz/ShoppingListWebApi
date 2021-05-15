using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using Shared;
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
        private readonly IConfiguration _configuration;              

        public ListItemController(ShopingListDBContext context, IConfiguration configuration, IMapper mapper, 
             IMediator mediator)//, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _mediator = mediator;
            _configuration = configuration;
            
        }


        [HttpPost("AddListItem")]
        [SecurityLevel(2)]

        public async Task<ActionResult<ListItem>> AddListItemToListt(int parentId, [FromBody]ListItem item, int listAggregationId)
        {
            //Task task = WebApiHelper.SendMessageToUserAsync(listAggregationId, _context, _hubConnection);
                        
            if (!CheckIntegrityList(parentId,  listAggregationId)) return Forbid();


            var listItemEntity = _mapper.Map<ListItemEntity>(item);
            listItemEntity.ListId = parentId;

            _context.ListItems.Add(listItemEntity);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {

            }
            //var item = _mapper.Map<ListItem>(listItemEntity);
            item.ListItemId = listItemEntity.ListItemId;

            //task.GetAwaiter().GetResult();

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            //await _signarRService.SendRefreshMessageToUsersAsync(userList, "Add_ListItem", listItemEntity.ListItemId, listAggregationId, parentId);
            await _mediator.Publish(
                new AddEditSaveDeleteListItemEvent(userList, "Add_ListItem", listItemEntity.ListItemId, listAggregationId, parentId));


            return await Task.FromResult(item);
        }


        [HttpPost("DeleteListItem")]
        [SecurityLevel(1)]
        public async Task<ActionResult<int>> DeleteListItem(int ItemId, int listAggregationId)
        {
            if (!CheckIntegrityListItem(ItemId, listAggregationId)) return Forbid();

            _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));
            var amount = await _context.SaveChangesAsync();

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            await _mediator.Publish(new AddEditSaveDeleteListItemEvent(userList, "Delete_ListItem", ItemId, listAggregationId));


            return await Task.FromResult(amount);
        }

        [HttpPost("GetItemListItem")]
        [SecurityLevel(3)]
        public async Task<ActionResult<ListItem>> GetItemListItem(int ItemId, int listAggregationId)
        {
            if (!CheckIntegrityListItem(ItemId, listAggregationId)) return Forbid();

            var listItemEntity = _context.ListItems.Single(a => a.ListItemId == ItemId);

            var listItem = _mapper.Map<ListItem>(listItemEntity);

           // var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
           // await _signarRService.SendRefreshMessageToUsersAsync(userList);

            return await Task.FromResult(listItem);
        }

        bool CheckIntegrityListItem(int listItemId, int listAggregationId)
        {
            
            var  listItem = _context.ListItems.Where(a => a.ListItemId == listItemId).Include(a=>a.List).FirstOrDefault();

            bool exist = false;

            if (listItem != null)
            {
                _context.Entry(listItem).State = EntityState.Detached;
                exist = listItem.List.ListAggregatorId == listAggregationId;
            }
            return exist;
        }

        bool CheckIntegrityList(int listId, int listAggregationId)
        {

            var list = _context.Lists.Where(a => a.ListId == listId).FirstOrDefault();

            bool exist = false;

            if (list != null)
            {
                _context.Entry(list).State = EntityState.Detached;
                exist = list.ListAggregatorId == listAggregationId;
            }
            return exist;
        }


        [HttpPost("EditListItem")]
        //[Authorize]
        [SecurityLevel(2)]
        public async Task<ActionResult<ListItem>> EditListItem([FromBody]ListItem item, int listAggregationId)
        {           

            if (!CheckIntegrityListItem(item.ListItemId, listAggregationId)) return Forbid();

            var listItemEntity = _mapper.Map<ListItemEntity>(item);

            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));


            _context.Entry<ListItemEntity>(listItemEntity).Property(nameof(ListItemEntity.ListItemName)).IsModified =true;
            var amount = await _context.SaveChangesAsync();

            var listItem = _mapper.Map<ListItem>(listItemEntity);


            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            await _mediator.Publish(new AddEditSaveDeleteListItemEvent(userList, "Edit/Save_ListItem", listItem.ListItemId, listAggregationId));

            return await Task.FromResult(listItem);
        }

        [HttpPost("ChangeOrderListItem")]
        [Authorize]
        public async Task<ActionResult<bool>> ChangeOrderListItem([FromBody]IEnumerable<ListItem> items)
        {
            var listItemEntity = _mapper.Map<IEnumerable<ListItemEntity>>(items);


            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));

            foreach (var item in listItemEntity)
            {
                _context.Entry<ListItemEntity>(item).Property(nameof(ListItemEntity.Order)).IsModified = true;
            }
           

            var amount = await _context.SaveChangesAsync();

           // var listItem = _mapper.Map<ListItem>(listItemEntity);


            return await Task.FromResult(true);
        }

        [HttpPost("SaveProperty")]
        //[Authorize]
        [SecurityLevel(3)]
        public async Task<ActionResult<ListItem>> SaveProperty([FromBody]ListItem item, string propertyName, int listAggregationId)
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

            var users = await _mediator.Send(new GetUserIdFromListAggrIdCommand(listAggregationId));
            Debug.WriteLine(sw.ElapsedMilliseconds - t1);

           // await _signarRService.SendRefreshMessageToUsersAsync(users.Data, "Edit/Save_ListItem", item.ListItemId, listAggregationId);

            await _mediator.Publish(new AddEditSaveDeleteListItemEvent(users.Data, "Edit/Save_ListItem", item.ListItemId, listAggregationId));


            return await Task.FromResult(res.Data);
        }
    }
}