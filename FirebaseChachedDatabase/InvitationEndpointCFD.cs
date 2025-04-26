using AutoMapper;
using FirebaseDatabase;
using Microsoft.Extensions.Caching.Distributed;
using Shared.DataEndpoints;
using Shared.DataEndpoints.Abstaractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class InvitationEndpointCFD : IInvitationEndpoint
    {
        private readonly InvitationEndpointFD _invitation;
        private readonly CacheConveinient _cache;

        public InvitationEndpointCFD(IMapper mapper, InvitationEndpointFD invitation, CacheConveinient cache)
        {
            _invitation = invitation;
            _cache = cache;
        }

        public async Task AcceptInvitationAsync(Invitation invitation, int userId)
        {

            await _invitation.AcceptInvitationAsync(invitation, userId);

            var listUsAggr = await _cache.GetAsync<List<UserListAggregatorFD>>("userId_" + userId);

            if (listUsAggr != null)
            {
                listUsAggr.Add(new UserListAggregatorFD
                {
                    ListAggregatorId = invitation.ListAggregatorId,
                    PermissionLevel = invitation.PermissionLevel,
                    UserId = userId
                });

                await _cache.SetAsync("userId_" + userId, listUsAggr);
            }

            var listPermCached = await _cache.GetAsync<ListAggregationForPermission>("ListAggregationForPermission_" + invitation.ListAggregatorId);

            if (listPermCached != null)
            {
                listPermCached.Users.Add(new UserPermissionToListAggregation
                {
                    Permission = invitation.PermissionLevel
                    , User=new User { UserId=userId, EmailAddress=invitation.EmailAddress }

                }) ;

                await _cache.SetAsync("ListAggregationForPermission_" + invitation.ListAggregatorId, listPermCached);
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
