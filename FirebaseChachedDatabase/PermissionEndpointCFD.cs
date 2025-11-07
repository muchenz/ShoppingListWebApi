using FirebaseDatabase;
using Google.Protobuf.Reflection;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase;
internal class PermissionEndpointCFD : IPermissionEndpoint
{
    private readonly PermissionEndpointFD _permissionEndpoint;
    private readonly CacheConveinient _cache;

    public PermissionEndpointCFD(PermissionEndpointFD permissionEndpoint, CacheConveinient cache)
    {
        _permissionEndpoint = permissionEndpoint;
        _cache = cache;
    }


    public async Task<Result> ChangeUserPermission(int listAggregationId, UserPermissionToListAggregation item, int senderId)
    {
        var result = await _permissionEndpoint.ChangeUserPermission(listAggregationId, item, senderId);

        if (result.IsError)
        {
            return result;
        }

        var userId = item.User.UserId;
        var permission = item.Permission;

        await _cache.UpdateAsync<List<UserListAggregator>, string>(Dictionary.UserId + userId,
           userListAggr =>
           {
               userListAggr.First(a => a.UserId == userId && a.ListAggregatorId == listAggregationId)
               .PermissionLevel = permission;
               return Task.FromResult(userListAggr);
           });

        await _cache.UpdateAsync<List<UserPermissionToListAggregation>, string>(Dictionary.UserPermisionListByListAggrID + listAggregationId,

            (listUsersPermToListaggr) =>
            {
                listUsersPermToListaggr.First(a => a.User.UserId == userId).Permission = permission;

                return Task.FromResult(listUsersPermToListaggr);
            });


        return result;
    }

    public async Task<Result> DeleteUserPermission(int listAggregationId, UserPermissionToListAggregation item, int senderId)
    {
        var result = await _permissionEndpoint.DeleteUserPermission(listAggregationId, item, senderId);

        if (result.IsError)
        {
            return result;
        }
        var userId = item.User.UserId;

        await _cache.UpdateAsync<List<UserListAggregator>, string>(Dictionary.UserId + userId,
              userListAggr =>
              {
                  userListAggr.Remove(
                   userListAggr.First(a => a.UserId == userId && a.ListAggregatorId == listAggregationId));

                  return Task.FromResult(userListAggr);
              });

        await _cache.UpdateAsync<List<UserPermissionToListAggregation>, string>(Dictionary.UserPermisionListByListAggrID + listAggregationId,

             (listUsersPermToListaggr) =>
             {
                 listUsersPermToListaggr.Remove(listUsersPermToListaggr.First(a => a.User.UserId == userId));

                 return Task.FromResult(listUsersPermToListaggr);
             });



        return result;
    }

    public async Task<Result<(User InvitedUser, Invitation Invitation)>> InviteUserPermission(int listAggregationId, int permissionLvl, string userName, string senderName, int senderId)
    {
        var result = await _permissionEndpoint.InviteUserPermission(listAggregationId, permissionLvl, userName, senderName, senderId);
              

        return result;
    }
}
