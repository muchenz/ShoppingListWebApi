using AutoMapper;
using EFDataBase;
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
      
       // public  SignarRService SignarRService=>_signarRService;


    }

    public class CreateAddListCommandHandler : IHandlerWrapper<AddListCommand, List>
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;

        public CreateAddListCommandHandler(ShopingListDBContext context, IMapper mapper )
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<MessageAndStatusAndData<List>> Handle(AddListCommand request, CancellationToken cancellationToken)
        {

            if (!CheckIntegrityListAggr(request.ParentId, request.ListAggregationId)) 
                return await Task.FromResult(MessageAndStatusAndData.Fail<List>("Forbbidden."));


            var listItemEntity = _mapper.Map<EFDataBase.ListEntity>(request.Item);
            listItemEntity.ListAggregatorId = request.ParentId;

            _context.Lists.Add(listItemEntity);
            await _context.SaveChangesAsync();

            //var item = _mapper.Map<ListItem>(listItemEntity);
            request.Item.ListId = listItemEntity.ListId;

            //var userList = await WebApiHelper.GetuUserIdFromListAggrIdAsync(request.ListAggregationId, request.Context);
            //await request.SignarRService.SendRefreshMessageToUsersAsync(userList);

            return await Task.FromResult(MessageAndStatusAndData.Ok<List>(request.Item,"OK"));


            //return Task.FromResult(MessageAndStatusAndData.Fail<List>(""));
        }

        bool CheckIntegrityListAggr(int listAggr, int listAggregationId)
        {

            return listAggr == listAggregationId;
        }

        public static async Task<IEnumerable<int>> GetuUserIdFromListAggrIdAsync(int listAggrId, ShopingListDBContext _context)
        {
            var userList = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggrId).Select(a => a.UserId).ToListAsync();

            return userList;
        }
    }


}
