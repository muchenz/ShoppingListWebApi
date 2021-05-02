using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly SignarRService _signarRService;
        private readonly IConfiguration _configuration;
        public ListAggregatorController(ShopingListDBContext context, IConfiguration configuration, IMapper mapper, SignarRService signarRService)//, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _signarRService = signarRService;
            _configuration = configuration;
        }

       
        [HttpPost("AddListAggregator")]
       // [SecurityLevel(2)]
       [Authorize]
        public async Task<ActionResult<ListAggregator>> AddListAggregatortoListt(int parentId, [FromBody]ListAggregator item)
        {

            var listItemEntity = _mapper.Map<ListAggregatorEntity>(item);
            //listItemEntity.ListAggregatorId = parentId;

            var userListAggregatorEntity = new UserListAggregatorEntity { UserId = parentId, ListAggregator = listItemEntity, PermissionLevel=1 };

            
            _context.UserListAggregators.Add(userListAggregatorEntity);
            
            await _context.SaveChangesAsync();
            

            //var item = _mapper.Map<ListItem>(listItemEntity);
            item.ListAggregatorId = listItemEntity.ListAggregatorId;

            

            return await Task.FromResult(item);
        }

        [HttpPost("DeleteListAggregator")]
        [SecurityLevel(1)]
        public async Task<ActionResult<int>> DeleteList(int ItemId, int listAggregationId)
        {

            _context.ListAggregators.Remove(_context.ListAggregators.Single(a => a.ListAggregatorId == ItemId));
            var amount = await _context.SaveChangesAsync();

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            await _signarRService.SendRefreshMessageToUsersAsync(userList);

            return await Task.FromResult(amount);
        }


        bool CheckIntegrity(int itemId, int listAggregationId)
        {
            bool aaa =  itemId == listAggregationId;

            return aaa;
        }

        [HttpPost("EditListAggregator")]
        [SecurityLevel(2)]
        public async Task<ActionResult<ListAggregator>> EditListAggregator([FromBody]ListAggregator item, int listAggregationId)
        {
            var listItemEntity = _mapper.Map<ListAggregatorEntity>(item);

            if (!CheckIntegrity(item.ListAggregatorId, listAggregationId)) return Forbid();

            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));


            _context.Entry<ListAggregatorEntity>(listItemEntity).Property(nameof(ListAggregatorEntity.ListAggregatorName)).IsModified = true;
            var amount = await _context.SaveChangesAsync();

            var listItem = _mapper.Map<ListAggregator>(listItemEntity);

            var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(listAggregationId, _context);
            await _signarRService.SendRefreshMessageToUsersAsync(userList);


            return await Task.FromResult(listItem);
        }

        [HttpPost("ChangeOrderListAggregator")]
        [Authorize]
        public async Task<ActionResult<bool>> ChangeOrderListItem([FromBody]IEnumerable<ListAggregator> items)
        {
            var listItemEntity = _mapper.Map<IEnumerable<ListAggregatorEntity>>(items);


            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));

            foreach (var item in listItemEntity)
            {
                _context.Entry<ListAggregatorEntity>(item).Property(nameof(ListAggregatorEntity.Order)).IsModified = true;
            }


            var amount = await _context.SaveChangesAsync();

            // var listItem = _mapper.Map<ListItem>(listItemEntity);

            return await Task.FromResult(true);
        }
    }
}