using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataBase
{
    public class ListEndpoint : IListEndpoint
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private object request;

        public ListEndpoint(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List> AddListAsync(int parentId, List list, int ListAggregationId)
        {
            var listItemEntity = _mapper.Map<ListEntity>(list);
            listItemEntity.ListAggregatorId = parentId;

            _context.Lists.Add(listItemEntity);
            await _context.SaveChangesAsync();

            list.ListId = listItemEntity.ListId;

            return list;
        }

        public Task<bool> CheckIntegrityListAggrAsync(int listAggrId, int listAggregationId)
        {
            return Task.FromResult(listAggrId == listAggregationId);
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

        public async Task<int> DeleteListAsync(int listId, int listAggregationId)
        {
            _context.Lists.Remove(_context.Lists.Single(a => a.ListId == listId));

            var amount = await _context.SaveChangesAsync();

            return amount;
        }

        public async Task<List> EditListAsync(List list)
        {
            var listItemEntity = _mapper.Map<ListEntity>(list);

            _context.Entry(listItemEntity).Property(nameof(ListEntity.ListName)).IsModified = true;
            var amount = await _context.SaveChangesAsync();

            var listItem = _mapper.Map<List>(listItemEntity);

            return listItem;
        }
    }
}
