using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataBase
{
    public class ListAggregatorEndpoint : IListAggregatorEndpoint
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;

        public ListAggregatorEndpoint(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ListAggregator> AddListAggregatorAsync(ListAggregator listAggregator, int parenId)
        {

            var listItemEntity = _mapper.Map<ListAggregatorEntity>(listAggregator);
            //listItemEntity.ListAggregatorId = parentId;

            var userListAggregatorEntity = new UserListAggregatorEntity { UserId = parenId, ListAggregator = listItemEntity, PermissionLevel = 1 };


            _context.UserListAggregators.Add(userListAggregatorEntity);

            await _context.SaveChangesAsync();

            return _mapper.Map<ListAggregator>(listItemEntity);
        }


        public async Task<int> DeleteListAggrAsync(int listAggregationId)
        {
            _context.ListAggregators.Remove(_context.ListAggregators.Single(a => a.ListAggregatorId == listAggregationId));
            var amount = await _context.SaveChangesAsync();

            return amount;
        }

        public async Task<ListAggregator> EditListAggregatorAsync(ListAggregator listAggregator)
        {
            var listItemEntity = _mapper.Map<ListAggregatorEntity>(listAggregator);
            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));


            _context.Entry<ListAggregatorEntity>(listItemEntity).Property(nameof(ListAggregatorEntity.ListAggregatorName)).IsModified = true;
            await _context.SaveChangesAsync();

            var listItem = _mapper.Map<ListAggregator>(listItemEntity);

            return listItem;
        }


        public async Task ChangeOrderListItemAsync(IEnumerable<ListAggregator> items)
        {
            var listItemEntity = _mapper.Map<IEnumerable<ListAggregatorEntity>>(items);


            // _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));

            foreach (var item in listItemEntity)
            {
                _context.Entry<ListAggregatorEntity>(item).Property(nameof(ListAggregatorEntity.Order)).IsModified = true;
            }


            await _context.SaveChangesAsync();
        }
    }
}
