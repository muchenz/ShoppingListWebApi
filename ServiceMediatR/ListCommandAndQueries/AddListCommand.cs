using AutoMapper;
using EFDataBase;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServiceMediatR.Models;
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
    public class AddListCommand : IRequestWrapper<List> {
           
        private readonly int _parentId;
        private readonly List _item;
        private readonly int _listAggregationId;

        public AddListCommand(
          

            int parentId,  List item, int listAggregationId)
        {
          
            _parentId = parentId;
            _item = item;
            _listAggregationId = listAggregationId;
        }

        public int ParentId=>_parentId;
        public List Item=>_item;
        public int ListAggregationId=>_listAggregationId;    
       
    }

   // public class CreateAddListCommandHandler : BaseForHandler,  IHandlerWrapper<AddListCommand, List>
    public class CreateAddListCommandHandler :   IHandlerWrapper<AddListCommand, List>
    {
        private readonly IListEndpoint _listEndpoint;

        //public CreateAddListCommandHandler(ShopingListDBContext context, IMapper mapper, IListEndpoint listEndpoint):base(context, mapper)
        public CreateAddListCommandHandler(IListEndpoint listEndpoint)
        {
            _listEndpoint = listEndpoint;
        }

        public async Task<MessageAndStatusAndData<List>> Handle(AddListCommand request, CancellationToken cancellationToken)
        {

            if (!await _listEndpoint.CheckIntegrityListAggrAsync(request.ParentId, request.ListAggregationId)) 
                return await Task.FromResult(MessageAndStatusAndData.Fail<List>("Forbbidden."));


            var res =  await _listEndpoint.AddListAsync(request.ParentId, request.Item, request.ListAggregationId);


            return await Task.FromResult(MessageAndStatusAndData.Ok<List>(res, "OK"));

        }       

    }


}
