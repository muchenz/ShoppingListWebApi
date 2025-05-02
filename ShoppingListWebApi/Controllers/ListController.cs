using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ServiceMediatR.ListCommandAndQueries;
using ServiceMediatR.SignalREvents;
using ServiceMediatR.UserCommandAndQuerry;
using Shared.DataEndpoints.Models;
using ShoppingListWebApi.Auth.Api;
using ShoppingListWebApi.Data;



namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListController : ControllerBase
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
       
        public ListController(IMapper mapper,
            IMediator mediator)//, IConfiguration configuration)
        {
            _mapper = mapper;
            _mediator = mediator;
           
        }


        [HttpPost("AddList")]
        [SecurityLevel(2)]

        public async Task<ActionResult<List>> AddListtoListt(int parentId, [FromBody]List item, int listAggregationId
            ,[FromHeader]string signalRId)
        {           

            var res = await _mediator.Send(new AddListCommand(parentId, item, listAggregationId));

            if (res.IsError) Forbid();

            var userList = await _mediator.Send(new GetUserIdFromListAggrIdCommand(listAggregationId, User));

            await _mediator.Publish(new DataChangedEvent(userList.Data, signalRId));

            return await Task.FromResult(res.Data);

        }

        [HttpPost("DeleteList")]
        [SecurityLevel(1)]
        public async Task<ActionResult<int>> DeleteList(int ItemId, int listAggregationId, [FromHeader]string signalRId)
        {
            var res = await _mediator.Send(new DeleteListCommand(ItemId, listAggregationId));

            if (res.IsError) Forbid();

            var userList = await _mediator.Send(new GetUserIdFromListAggrIdCommand(listAggregationId, User));

            await _mediator.Publish(new DataChangedEvent(userList.Data, signalRId));

            return await Task.FromResult(res.Data);
                       
        }
              

        [HttpPost("EditList")]
        [SecurityLevel(2)]

        public async Task<ActionResult<List>> EditListItem([FromBody]List item, int listAggregationId
            , [FromHeader]string signalRId)
        {

            var res = await _mediator.Send(new EditListCommand(item, listAggregationId));

            if (res.IsError) return Forbid();


            var userList = await _mediator.Send(new GetUserIdFromListAggrIdCommand(listAggregationId, User));
            await _mediator.Publish(new DataChangedEvent(userList.Data, signalRId));

            return await Task.FromResult(res.Data);
        }

        [HttpPost("ChangeOrderList")]
        [Authorize]
        public async Task<ActionResult<bool>> ChangeOrderListItem([FromBody]IEnumerable<List> items)
        {
            var listItemEntity = _mapper.Map<IEnumerable<ListEntity>>(items);


            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));

            foreach (var item in listItemEntity)
            {
                _context.Entry<ListEntity>(item).Property(nameof(ListEntity.Order)).IsModified = true;
            }


            var amount = await _context.SaveChangesAsync();

            // var listItem = _mapper.Map<ListItem>(listItemEntity);

            return await Task.FromResult(true);
        }
    }
}