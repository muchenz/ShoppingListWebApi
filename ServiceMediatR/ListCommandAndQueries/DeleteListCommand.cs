using AutoMapper;
using EFDataBase;
using Microsoft.EntityFrameworkCore;
using ServiceMediatR.Wrappers;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
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


    //public class DeleteListHandler :BaseForHandler, IHandlerWrapper<DeleteListCommand, int>
    public class DeleteListHandler :IHandlerWrapper<DeleteListCommand, int>
    {
        private readonly IListEndpoint _listEndpoint;

        //public DeleteListHandler(ShopingListDBContext context, IMapper mapper, IListEndpoint listEndpoint):base(context, mapper)
        public DeleteListHandler(IListEndpoint listEndpoint)
        {
            _listEndpoint = listEndpoint;
        }

        public async Task<MessageAndStatusAndData<int>> Handle(DeleteListCommand request, CancellationToken cancellationToken)
        {

            if (!await _listEndpoint.CheckIntegrityListAsync(request.ItemId,  request.ListAggregationId)) return MessageAndStatusAndData<int>.Fail("Forbbidden");


           var amount = await _listEndpoint.DeleteListAsync(request.ItemId, request.ListAggregationId);

            return MessageAndStatusAndData<int>.Ok(amount);
        }
       
    }
}
