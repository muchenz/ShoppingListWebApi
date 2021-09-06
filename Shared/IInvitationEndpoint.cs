using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public interface IInvitationEndpoint
    {
        Task<List<Invitation>> GetInvitationsListAsync(string userName);
        Task RejectInvitaionAsync(Invitation invitation);
        Task AcceptInvitationAsync(Invitation invitation, int userId);
    }
}
