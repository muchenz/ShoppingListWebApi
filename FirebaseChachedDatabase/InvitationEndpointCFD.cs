using AutoMapper;
using FirebaseDatabase;
using Microsoft.Extensions.Caching.Distributed;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
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

            var listUsAggr = await _cache.GetAsync<List<UserListAggregatorFD>>(Dictionary.UserId + userId);

            if (listUsAggr != null)
            {
                listUsAggr.Add(new UserListAggregatorFD
                {
                    ListAggregatorId = invitation.ListAggregatorId,
                    PermissionLevel = invitation.PermissionLevel,
                    UserId = userId
                });

                await _cache.SetAsync(Dictionary.UserId + userId, listUsAggr);
            }

            var listPermCached = await _cache.GetAsync<List<UserPermissionToListAggregation>>(Dictionary.UserPermisionListByListAggrID
                + invitation.ListAggregatorId);

            if (listPermCached != null)
            {
                listPermCached.Add(new UserPermissionToListAggregation
                {
                    Permission = invitation.PermissionLevel
                    , User=new User { UserId=userId, EmailAddress=invitation.EmailAddress }

                }) ;

                await _cache.SetAsync(Dictionary.UserPermisionListByListAggrID + invitation.ListAggregatorId, listPermCached);
            }

        }

        public Task<List<Invitation>> GetInvitationsListAsync(int userId)
        {
            return _invitation.GetInvitationsListAsync(userId);
        }

        public Task RejectInvitaionAsync(Invitation invitation)
        {
            return _invitation.RejectInvitaionAsync(invitation);
        }
    }

}
