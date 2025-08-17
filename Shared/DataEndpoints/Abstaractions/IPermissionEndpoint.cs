using Shared.DataEndpoints.Models;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Abstaractions;
public interface IPermissionEndpoint
{
    Task<MessageAndStatus> InviteUserPermission(int listAggregationId, UserPermissionToListAggregation item, string senderName);
}