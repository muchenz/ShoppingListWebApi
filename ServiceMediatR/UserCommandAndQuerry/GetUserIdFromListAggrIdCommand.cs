using EFDataBase;
using Microsoft.EntityFrameworkCore;
using ServiceMediatR.Wrappers;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceMediatR.UserCommandAndQuerry
{
    public class GetUserIdFromListAggrIdCommand: IRequestWrapper<IEnumerable<int>>
    {
        public GetUserIdFromListAggrIdCommand(int listAggrId, ClaimsPrincipal user)
        {
            ListAggrId = listAggrId;
            User = user;
        }

        public int ListAggrId { get; }
        public ClaimsPrincipal User { get; }
    }


    public class GetUserIdFromListAggrIdCommandHandler : IHandlerWrapper<GetUserIdFromListAggrIdCommand, IEnumerable<int>>
    {
        private readonly ShopingListDBContext _context;

        public GetUserIdFromListAggrIdCommandHandler(ShopingListDBContext context)
        {
            _context = context;
        }
        public async Task<MessageAndStatusAndData<IEnumerable<int>>> Handle(GetUserIdFromListAggrIdCommand request, CancellationToken cancellationToken)
        {
            var userList = await _context.UserListAggregators.Where(a => a.ListAggregatorId == request.ListAggrId).Select(a => a.UserId).ToListAsync();


            var userId = request.User?.Claims?.Where(a => a.Type == ClaimTypes.NameIdentifier).SingleOrDefault().Value;

            if (userId != null)
                userList.Remove(int.Parse(userId));

            return MessageAndStatusAndData.Ok(userList.AsEnumerable(),"Ok");
        }
    }
}
