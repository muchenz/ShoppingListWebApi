using AutoMapper;
using EFDataBase;
using ServiceMediatR.Wrappers;
using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.ListCommandAndQueries
{
    public class EditListCommand: IRequestWrapper<List>
    {
        public EditListCommand(List list, int listAggregationId)
        {
            List = list;
            ListAggregationId = listAggregationId;
        }

        public List List { get; }
        public int ListAggregationId { get; }
    }


    public class EditListHandler : BaseForHandler, IHandlerWrapper<EditListCommand, List>
    {
        public EditListHandler(ShopingListDBContext context, IMapper mapper) : base(context, mapper)
        {

        }
        public async Task<MessageAndStatusAndData<List>> Handle(EditListCommand request, CancellationToken cancellationToken)
        {
            var listItemEntity = _mapper.Map<ListEntity>(request.List);


            if (!CheckIntegrityList(request.List.ListId, request.ListAggregationId)) return MessageAndStatusAndData.Fail<List>("Forbbidden");

            //_context.ListItems.Remove(_context.ListItems.Single(a => a.ListItemId == ItemId));

            _context.Entry<ListEntity>(listItemEntity).Property(nameof(ListEntity.ListName)).IsModified = true;
            var amount = await _context.SaveChangesAsync();

            var listItem = _mapper.Map<List>(listItemEntity);

            return await Task.FromResult(MessageAndStatusAndData.Ok(listItem, "OK"));

        }
    }
}
