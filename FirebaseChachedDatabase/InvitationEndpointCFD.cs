using AutoMapper;
using FirebaseDatabase;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class InvitationEndpointCFD : IInvitationEndpoint
    {
        private readonly InvitationEndpointFD _invitation;
        private readonly IDistributedCache _cache;

        public InvitationEndpointCFD(IMapper mapper, InvitationEndpointFD invitation, IDistributedCache cache)
        {
            _invitation = invitation;
            _cache = cache;
        }

        public async Task AcceptInvitationAsync(Invitation invitation, int userId)
        {

            await _invitation.AcceptInvitationAsync(invitation, userId);

            var listUsAggr = await _cache.GetOrAddAsync("userId_" + userId, _ => Task.FromResult(
                  new List<UserListAggregatorFD>()));

            if (listUsAggr.Cached)
            {
                listUsAggr.value.Add(new UserListAggregatorFD
                {
                    ListAggregatorId = invitation.ListAggregatorId,
                    PermissionLevel = invitation.PermissionLevel,
                    UserId = userId
                });

                await _cache.SetAsync("userId_" + userId, listUsAggr.value);
            }
        }

        public Task<List<Invitation>> GetInvitationsListAsync(string userName)
        {
            return _invitation.GetInvitationsListAsync(userName);
        }

        public Task RejectInvitaionAsync(Invitation invitation)
        {
            return _invitation.RejectInvitaionAsync(invitation);
        }
    }

}
