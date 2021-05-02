using AutoMapper;
using EFDataBase;
using Microsoft.EntityFrameworkCore;
using ServiceMediatR.Wrappers;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.ListItemCommandAndQueries
{
    public class SavePropertyCommand: IRequestWrapper<ListItem> 
    {
        public SavePropertyCommand(ListItem item, string propertyName, int listAggregationId)
        {
            Item = item;
            PropertyName = propertyName;
            ListAggregationId = listAggregationId;
        }

        public ListItem Item { get; }
        public string PropertyName { get; }
        public int ListAggregationId { get; }
    }


    public class SavePropertyCommandHandler : IHandlerWrapper<SavePropertyCommand, ListItem>
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;

        public SavePropertyCommandHandler(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<MessageAndStatusAndData<ListItem>> Handle(SavePropertyCommand request, CancellationToken cancellationToken)
        {
          
            if (!CheckIntegrityListItem(request.Item.ListItemId, request.ListAggregationId))
                return await Task.FromResult(MessageAndStatusAndData.Fail<ListItem>("Forbbidden"));

          
            var listItemEntity = _mapper.Map<ListItemEntity>(request.Item);


            _context.Entry<ListItemEntity>(listItemEntity).Property(request.PropertyName).IsModified = true;
            var amount = await _context.SaveChangesAsync();
          
            var listItem = _mapper.Map<ListItem>(listItemEntity);
                       


            return await Task.FromResult(MessageAndStatusAndData.Ok(listItem,"OK"));
        }

        bool CheckIntegrityListItem(int listItemId, int listAggregationId)
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
    }
}
