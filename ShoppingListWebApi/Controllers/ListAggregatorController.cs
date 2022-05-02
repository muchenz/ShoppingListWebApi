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
using ServiceMediatR.SignalREvents;
using Shared;
using ShoppingListWebApi.Data;



namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListAggregatorController : ControllerBase
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;
        private readonly IListAggregatorEndpoint _listAggregatorEndpoint;
        private readonly IUserEndpoint _userEndpoint;
        private readonly IConfiguration _configuration;
        public ListAggregatorController(IConfiguration configuration, IMapper mapper
            , IMediator mediator, IListAggregatorEndpoint listAggregatorEndpoint
            ,IUserEndpoint userEndpoint)//, IConfiguration configuration)
        {
            _mapper = mapper;
            _mediator = mediator;
            _listAggregatorEndpoint = listAggregatorEndpoint;
            _userEndpoint = userEndpoint;
            _configuration = configuration;
        }

       
        [HttpPost("AddListAggregator")]
       // [SecurityLevel(2)]
       [Authorize]
        public async Task<ActionResult<ListAggregator>> AddListAggregatortoListt(int parentId, [FromBody]ListAggregator item)
        {
            var listAggr = await _listAggregatorEndpoint.AddListAggregatorAsync(item, parentId);

            item.ListAggregatorId = listAggr.ListAggregatorId;

            return await Task.FromResult(item);
        }

        [HttpPost("DeleteListAggregator")]
        [SecurityLevel(1)]
        public async Task<ActionResult<int>> DeleteList(int ItemId, int listAggregationId,[FromHeader]string signalRId)
        {

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _userEndpoint, User);

            var amount = await _listAggregatorEndpoint.DeleteListAggrAsync(ItemId);
            await _mediator.Publish(new DataChangedEvent(userList, signalRId));

            return await Task.FromResult(amount);
        }


        bool CheckIntegrity(int itemId, int listAggregationId)
        {
            bool aaa =  itemId == listAggregationId;

            return aaa;
        }

        [HttpPost("EditListAggregator")]
        [SecurityLevel(2)]
        public async Task<ActionResult<ListAggregator>> EditListAggregator([FromBody]ListAggregator item, int listAggregationId
            ,[FromHeader]string signalRId)
        {

            if (!CheckIntegrity(item.ListAggregatorId, listAggregationId)) return Forbid();

            var listItem = await _listAggregatorEndpoint.EditListAggregatorAsync(item);

             var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _userEndpoint, User);

            await _mediator.Publish(new DataChangedEvent(userList, signalRId));


            return await Task.FromResult(listItem);
        }

        [HttpPost("ChangeOrderListAggregator")]
        [Authorize]
        public async Task<ActionResult<bool>> ChangeOrderListItem([FromBody]IEnumerable<ListAggregator> items)
        {

            await _listAggregatorEndpoint.ChangeOrderListItemAsync(items);
            // var listItem = _mapper.Map<ListItem>(listItemEntity);

            return await Task.FromResult(true);
        }
    }
}