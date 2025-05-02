using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Abstaractions
{
    public interface IInvitationEndpoint
    {
        Task<List<Invitation>> GetInvitationsListAsync(string userName);
        Task RejectInvitaionAsync(Invitation invitation);
        Task AcceptInvitationAsync(Invitation invitation, int userId);
    }
}
