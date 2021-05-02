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
using Shared;
using ShoppingListWebApi.Data;



namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListController : ControllerBase
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly SignarRService _signarRService;
        private readonly IMediator _mediator;
       
        public ListController(ShopingListDBContext context, IMapper mapper,
            SignarRService signarRService, IMediator mediator)//, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _signarRService = signarRService;
            _mediator = mediator;
           
        }


        [HttpPost("AddList")]
        [SecurityLevel(2)]

        public async Task<ActionResult<List>> AddListtoListt(int parentId, [FromBody]List item, int listAggregationId)
        {
            //if (!CheckIntegrityListAggr(parentId, listAggregationId)) return Forbid();


            //var listItemEntity = _mapper.Map<ListEntity>(item);
            //listItemEntity.ListAggregatorId = parentId;

            //_context.Lists.Add(listItemEntity);
            //await _context.SaveChangesAsync();

            ////var item = _mapper.Map<ListItem>(listItemEntity);
            //item.ListId = listItemEntity.ListId;

            //var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            //await _signarRService.SendRefreshMessageToUsersAsync(userList);

            //return await Task.FromResult(item);

            var res = await _mediator.Send(new AddListCommand(parentId, item, listAggregationId));

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);



            await _signarRService.SendRefreshMessageToUsersAsync(userList);

            return await Task.FromResult(res.Data);

        }

        [HttpPost("DeleteList")]
        [SecurityLevel(1)]
        public async Task<ActionResult<int>> DeleteList(int ItemId, int listAggregationId)
        {
            if (!CheckIntegrity(ItemId, listAggregationId)) return Forbid();


            _context.Lists.Remove(_context.Lists.Single(a => a.ListId == ItemId));
            var amount = await _context.SaveChangesAsync();

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            await _signarRService.SendRefreshMessageToUsersAsync(userList);

            return await Task.FromResult(amount);
        }

        bool CheckIntegrity(int listId, int listAggregationId)
        {
            var  list = _context.Lists.Where(a => a.ListId == listId).FirstOrDefault();

            bool aaa=false;

            if (list != null)
            {
                aaa = list.ListAggregatorId == listAggregationId;
                _context.Entry(list).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            
            return aaa;
        }

        bool CheckIntegrityListAggr(int listAggr, int listAggregationId)
        {

            return listAggr== listAggregationId;
        }

        [HttpPost("EditList")]
        [SecurityLevel(2)]

        public async Task<ActionResult<List>> EditListItem([FromBody]List item, int listAggregationId)
        {
            var listItemEntity = _mapper.Map<ListEntity>(item);


            if (!CheckIntegrity(item.ListId, listAggregationId)) return Forbid();

            //_context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));


            _context.Entry<ListEntity>(listItemEntity).Property(nameof(ListEntity.ListName)).IsModified = true;
            var amount = await _context.SaveChangesAsync();

            var listItem = _mapper.Map<List>(listItemEntity);

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            await _signarRService.SendRefreshMessageToUsersAsync(userList);

            return await Task.FromResult(listItem);
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