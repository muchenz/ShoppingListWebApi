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

    public class CreateAddListCommandHandler : BaseForHandler,  IHandlerWrapper<AddListCommand, List>
    {

        public CreateAddListCommandHandler(ShopingListDBContext context, IMapper mapper):base(context, mapper)
        {
        }

        public async Task<MessageAndStatusAndData<List>> Handle(AddListCommand request, CancellationToken cancellationToken)
        {

            if (!CheckIntegrityListAggr(request.ParentId, request.ListAggregationId)) 
                return await Task.FromResult(MessageAndStatusAndData.Fail<List>("Forbbidden."));


            var listItemEntity = _mapper.Map<EFDataBase.ListEntity>(request.Item);
            listItemEntity.ListAggregatorId = request.ParentId;

            _context.Lists.Add(listItemEntity);
            await _context.SaveChangesAsync();

            request.Item.ListId = listItemEntity.ListId;


            return await Task.FromResult(MessageAndStatusAndData.Ok<List>(request.Item,"OK"));

        }       

    }


}
