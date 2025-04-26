using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.DataEndpoints;
using Shared.DataEndpoints.Abstaractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataBase
{
    public class ListItemEndpoint : IListItemEndpoint
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;

        public ListItemEndpoint(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ListItem> AddListItemAsync(int parentId, ListItem listItem, int listAggregationId)
        {
            var listItemEntity = _mapper.Map<ListItemEntity>(listItem);
            listItemEntity.ListId = parentId;

            _context.ListItems.Add(listItemEntity);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

            }

            var item = _mapper.Map<ListItem>(listItemEntity);
            return item;

        }

        public async Task<int> DeleteListItemAsync(int listItemId, int listAggregationId)
        {
            _context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == listItemId));
            var amount = await _context.SaveChangesAsync();
            return amount;
        }

        public async Task<ListItem> GetItemListItemAsync(int listItemId)
        {
            var listItemEntity = await _context.ListItems.SingleAsync(a => a.ListItemId == listItemId);

            var listItem = _mapper.Map<ListItem>(listItemEntity);

            return listItem;
        }
        public async Task<bool> CheckIntegrityListItemAsync(int listItemId, int listAggregationId)
        {
            var listItem = await _context.ListItems.AsQueryable().Where(a => a.ListItemId == listItemId).Include(a => a.List).FirstOrDefaultAsync();

            bool exist = false;

            if (listItem != null)
            {
                _context.Entry(listItem).State = EntityState.Detached;
                exist = listItem.List.ListAggregatorId == listAggregationId;
            }
            return exist;
        }

        public async Task<bool> CheckIntegrityListAsync(int listId, int listAggregationId)
        {
            var list = await _context.Lists.AsQueryable().Where(a => a.ListId == listId).FirstOrDefaultAsync();

            bool exist = false;

            if (list != null)
            {
                _context.Entry(list).State = EntityState.Detached;
                exist = list.ListAggregatorId == listAggregationId;
            }
            return exist;
        }

        public async Task<ListItem> EditListItemAsync(ListItem listItem, int listAggregationId)
        {
            var listItemEntity = _mapper.Map<ListItemEntity>(listItem);

            _context.Entry<ListItemEntity>(listItemEntity).Property(nameof(ListItemEntity.ListItemName)).IsModified = true;
            var amount = await _context.SaveChangesAsync();

            listItem = _mapper.Map<ListItem>(listItemEntity);

            return listItem;

        }

        public async Task<int> ChangeOrderListItemAsync(IEnumerable<ListItem> items)
        {
            var listItemEntity = _mapper.Map<IEnumerable<ListItemEntity>>(items);

            foreach (var item in listItemEntity)
            {
                _context.Entry<ListItemEntity>(item).Property(nameof(ListItemEntity.Order)).IsModified = true;
            }

            var amount = await _context.SaveChangesAsync();
            return amount;
        }

        public async Task<ListItem> SavePropertyAsync(ListItem listItem, string propertyName, int listAggregationId)
        {
            var listItemEntity = _mapper.Map<ListItemEntity>(listItem);


            _context.Entry<ListItemEntity>(listItemEntity).Property(propertyName).IsModified = true;
            await _context.SaveChangesAsync();

            return _mapper.Map<ListItem>(listItemEntity);
        }
    }
}
