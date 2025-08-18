using Shared.DataEndpoints.Models;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Abstaractions;
public interface IPermissionEndpoint
{
    Task<MessageAndStatusAndData<(User InvitedUser, Invitation Invitation)>> 
            InviteUserPermission(int listAggregationId, UserPermissionToListAggregation item, string senderName, int senderId);
}