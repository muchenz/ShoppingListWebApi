using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataEndpoints.Abstaractions
{
    public interface IUserEndpoint
    {
        public Task<User> FindUserByIdAsync(int id);

        Task<User> GetTreeAsync(string userName);
        Task<User> GetUserByNameAsync(string userName);

        Task<bool> IsUserHasListAggregatorAsync(int userId, int listAggregatorId);

        Task AddUserListAggregationAsync(int userId, int listAggregationId, int permission);

        Task<bool> IsUserInvitatedToListAggregationAsync(string userName, int listAggregationId);

        Task AddInvitationAsync(string toUserName, int listAggregationId, int permission, string fromSenderName);

        Task<int> GetNumberOfAdministratorsOfListAggregationsAsync(int listAggregationId); 
        Task<int> GetLastAdminIdAsync(int listAggregationId);
        Task SetUserPermissionToListAggrAsync(int userId, int listAggregationId, int permission);
        Task DeleteUserListAggrAscync(int userId, int listAggregationId);

        Task<List<ListAggregationForPermission>> GetListAggregationForPermissionAsync(string userName);
        Task<List<ListAggregationForPermission>> GetListAggregationForPermission2Async(string userName);
        Task<List<ListAggregationForPermission>> GetListAggregationForPermission_EmptyAsync(int userId);
        Task<ListAggregationForPermission> GetListAggregationForPermissionByListAggrIdAsync(ListAggregationForPermission listAggregationForPermission);

        Task<List<UserListAggregator>> GetUserListAggrByUserId(int userId);

        Task<List<string>> GetUserRolesByUserIdAsync(int userId);

        Task<User> GetUserWithRolesAsync(int userId);

        Task<User> LoginAsync(string userName, string password);
        Task<User> Register(string userName, string password, LoginType loginType);
        Task<List<int>> GetUserIdsFromListAggrIdAsync(int listAggregationId);

    }

    public interface IUserEndpointFD
    {
        public Task<User> FindUserByIdAsync(int id);

        Task<User> GetTreeAsync(string userName);
        Task<User> GetUserByNameAsync(string userName);

        Task<bool> IsUserHasListAggregatorAsync(int userId, int listAggregatorId);

        Task AddUserListAggregationAsync(int userId, int listAggregationId, int permission);

        Task<bool> IsUserInvitatedToListAggregationAsync(string userName, int listAggregationId);

        Task AddInvitationAsync(string toUserName, int listAggregationId, int permission, string fromSenderName);

        Task<int> GetNumberOfAdministratorsOfListAggregationsAsync(int listAggregationId);
        Task<int> GetLastAdminIdAsync(int listAggregationId);
        Task SetUserPermissionToListAggrAsync(int userId, int listAggregationId, int permission);
        Task DeleteUserListAggrAscync(int userId, int listAggregationId);

        Task<List<ListAggregationForPermission>> GetListAggregationForPermission(string userName);
        Task<List<ListAggregationForPermission>> GetListAggregationForPermission2(string userName);

        Task<List<UserListAggregator>> GetUserListAggrByUserId(int userId);

        Task<List<string>> GetUserRolesByUserIdAsync(int userId);

        Task<User> GetUserWithRolesAsync(int userId);

        Task<User> LoginAsync(string userName, string password);

        Task<User> Register(string userName, string password, LoginType loginType);
    }

    public enum LoginType { Local = 1, Facebook = 2 }

}
