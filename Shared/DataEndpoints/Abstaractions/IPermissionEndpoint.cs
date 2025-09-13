using Shared.DataEndpoints.Models;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Abstaractions;
public interface IPermissionEndpoint
{
    Task<Result<(User InvitedUser, Invitation Invitation)>> 
            InviteUserPermission(int listAggregationId, int permissionLvl, string userName, string senderName, int senderId);
}