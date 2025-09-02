using EFDataBase;
using Microsoft.EntityFrameworkCore;
using ServiceMediatR.Wrappers;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
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
        private readonly IUserEndpoint _userEndpoint;

        public GetUserIdFromListAggrIdCommandHandler(IUserEndpoint userEndpoint)
        {
            _userEndpoint = userEndpoint;
        }
        public async Task<Result<IEnumerable<int>>> Handle(GetUserIdFromListAggrIdCommand request, CancellationToken cancellationToken)
        {
            var userList = await _userEndpoint.GetUserIdsFromListAggrIdAsync(request.ListAggrId);


            var userId = request.User?.Claims?.Where(a => a.Type == ClaimTypes.NameIdentifier).SingleOrDefault().Value;

            //if (userId != null)
             //   userList.Remove(int.Parse(userId));

            return Result<IEnumerable<int>>.Ok(userList.AsEnumerable());
        }
    }
}
