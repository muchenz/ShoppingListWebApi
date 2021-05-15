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

namespace ServiceMediatR.ListCommandAndQueries
{
    public class DeleteListCommand : IRequestWrapper<int> 
    {

        public DeleteListCommand(int ItemId, int listAggregationId)
        {
            this.ItemId = ItemId;
            ListAggregationId = listAggregationId;
        }

        public int ItemId { get; }
        public int ListAggregationId { get; }
    }


    public class DeleteListHandler :BaseForHandler, IHandlerWrapper<DeleteListCommand, int>
    {

        public DeleteListHandler(ShopingListDBContext context, IMapper mapper):base(context, mapper)
        {
        }

        public async Task<MessageAndStatusAndData<int>> Handle(DeleteListCommand request, CancellationToken cancellationToken)
        {

            if (!CheckIntegrityList(request.ItemId,  request.ListAggregationId)) return MessageAndStatusAndData.Fail<int>("Forbbidden");


            _context.Lists.Remove(_context.Lists.Single(a => a.ListId == request.ItemId));
            var amount = await _context.SaveChangesAsync();


            return await Task.FromResult(MessageAndStatusAndData.Ok(amount, "OK"));
        }
       
    }
}
