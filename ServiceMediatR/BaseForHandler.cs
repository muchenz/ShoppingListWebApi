using AutoMapper;
using EFDataBase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceMediatR
{
    public class BaseForHandler
    {
        protected readonly ShopingListDBContext _context;
        protected readonly IMapper _mapper;

        public BaseForHandler(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


       protected bool CheckIntegrityListItem(int listItemId, int listAggregationId)
        {

            var listItem = _context.ListItems.Where(a => a.ListItemId == listItemId).Include(a => a.List).FirstOrDefault();

            bool exist = false;

            if (listItem != null)
            {
                _context.Entry(listItem).State = EntityState.Detached;
                exist = listItem.List.ListAggregatorId == listAggregationId;
            }
            return exist;
        }

        protected bool CheckIntegrityList(int listId, int listAggregationId)
        {
            var list = _context.Lists.Where(a => a.ListId == listId).FirstOrDefault();

            bool aaa = false;

            if (list != null)
            {
                aaa = list.ListAggregatorId == listAggregationId;
                _context.Entry(list).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }

            return aaa;
        }
        protected bool CheckIntegrityListAggr(int listAggr, int listAggregationId)
        {

            return listAggr == listAggregationId;
        }

    }
}
