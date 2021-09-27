using AutoMapper;
using FirebaseDatabase;
using Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public class InvitationEndpointCFD : IInvitationEndpoint
    {
        private readonly InvitationEndpointFD _invitation;

        public InvitationEndpointCFD(IMapper mapper, InvitationEndpointFD invitation) 
        {
            _invitation = invitation;
        }

        public Task AcceptInvitationAsync(Invitation invitation, int userId)
        {
            return _invitation.AcceptInvitationAsync(invitation, userId);
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
