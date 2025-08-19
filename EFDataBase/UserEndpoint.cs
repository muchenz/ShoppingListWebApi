using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EFDataBase
{
    public class UserEndpoint : IUserEndpoint, ITokenEndpoint
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;

        public UserEndpoint(ShopingListDBContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }



        public async Task<User> FindUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            return _mapper.Map<User>(user);
        }

        public async Task<User> GetTreeAsync(string userName)
        {
            UserEntity userDTO = null;
            try
            {
                userDTO = await _context.Users
                   // .Where(a => a.UserId == id)
                   .Include(a => a.UserRoles).ThenInclude(a => a.Role)
                   .Include(a => a.UserListAggregators)
                   .ThenInclude(a => a.ListAggregator)
                   .ThenInclude(a => a.Lists)
                   .ThenInclude(a => a.ListItems).Select(a => new UserEntity
                   {
                       UserId = a.UserId,
                       UserListAggregators = a.UserListAggregators,
                       EmailAddress = a.EmailAddress,
                       UserRoles = a.UserRoles

                   }).FirstOrDefaultAsync(a => a.EmailAddress == userName);



            }
            catch (Exception ex)
            {


            }

            var userTemp = _mapper.Map(userDTO, typeof(UserEntity), typeof(User)) as User;

            return userTemp;
        }

        public async Task<User> GetUserByNameAsync(string userName)
        {
            var user = await _context.Users.AsQueryable().Where(a => a.EmailAddress == userName).FirstOrDefaultAsync();

            if (user == null) return null;

            var userTemp = _mapper.Map<User>(user);

            return userTemp;

        }

        public Task<bool> IsUserHasListAggregatorAsync(int userId, int listAggregatorId)
        {
            return _context.UserListAggregators.AsQueryable()
                 .Where(a => a.User.UserId == userId && a.ListAggregatorId == listAggregatorId).AnyAsync();
        }
        public async Task<bool> IsUserIsAdminOfListAggregatorAsync(int userId, int listAggregatorId)
        {
            var users = await _context.UserListAggregators.AsQueryable()
                 .Where(a => a.User.UserId == userId && a.ListAggregatorId == listAggregatorId).ToListAsync();

            if (users.Count == 0) return false;

            return users.First().PermissionLevel == 1; // 1 is admin
        }
        public async Task AddUserListAggregationAsync(int userId, int listAggregationId, int permission)
        {

            var userListAggregatorEntity = new UserListAggregatorEntity
            {
                UserId = userId,
                ListAggregatorId = listAggregationId,
                PermissionLevel = permission
            };


            _context.UserListAggregators.Add(userListAggregatorEntity);

            await _context.SaveChangesAsync();
        }

        public Task<bool> IsUserInvitatedToListAggregationAsync(string userName, int listAggregationId)
        {
            return _context.Invitations.AsQueryable()
                .Where(a => a.EmailAddress == userName && a.ListAggregatorId == listAggregationId).AnyAsync();

        }

        public async Task AddInvitationAsync(string toUserName, int listAggregationId, int permission, string fromSenderName)
        {

            var invitationEntity = new InvitationEntity
            {
                EmailAddress = toUserName,
                ListAggregatorId = listAggregationId,
                PermissionLevel = permission,
                SenderName = fromSenderName
            };


            _context.Add(invitationEntity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserListAggregator>> TryGetTwoAdministratorsOfListAggregationsAsync(int listAggregationId)
        {
            return  await _context.UserListAggregators.AsQueryable().Where(a => a.ListAggregatorId == listAggregationId && a.PermissionLevel == 1)
                .Take(2).Select(a=>_mapper.Map<UserListAggregator>(a)).ToListAsync();
        }

        public async Task<int> GetLastAdminIdAsync(int listAggregationId)
        {
            return (await _context.UserListAggregators.AsQueryable()
                .Where(a => a.ListAggregatorId == listAggregationId && a.PermissionLevel == 1).SingleAsync()).UserId;
        }

        public async Task SetUserPermissionToListAggrAsync(int userId, int listAggregationId, int permission)
        {
            var userListAggr = await _context.UserListAggregators.AsQueryable().Where(a => a.User.UserId == userId && a.ListAggregatorId == listAggregationId)
                .FirstOrDefaultAsync();


            userListAggr.PermissionLevel = permission;


            //  _context.Update(userListAggr);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserListAggrAscync(int userId, int listAggregationId)
        {
            var userListAggr = await _context.UserListAggregators.AsQueryable().Where(a => a.User.UserId == userId && a.ListAggregatorId == listAggregationId)
                 .FirstOrDefaultAsync();

            _context.Remove(userListAggr);


            //  _context.Update(userListAggr);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPermAsync(string userName)
        {
            var userListAggregatorsEntities = await _context.UserListAggregators

                .Include(a => a.ListAggregator).Where(a => a.User.EmailAddress == userName && a.PermissionLevel == 1)
                .Select(a => a.ListAggregator).ToListAsync();


            var listAggregatinPermissionAndUser = new Dictionary<ListAggregatorEntity, List<KeyValuePair<UserEntity, int>>>();


            foreach (var item in userListAggregatorsEntities)
            {

                var user = _context.UserListAggregators.AsQueryable().Where(a => a.ListAggregatorId == item.ListAggregatorId)
                    .Select(a => new KeyValuePair<UserEntity, int>(a.User, a.PermissionLevel));

                var userList = await user.ToListAsync();

                listAggregatinPermissionAndUser.Add(item, userList);
            }

            List<ListAggregationWithUsersPermission> dataTransfer = ConvertFromEntitiesToDTOtListObject(listAggregatinPermissionAndUser);

            return dataTransfer;

        }
        private List<ListAggregationWithUsersPermission> ConvertFromEntitiesToDTOtListObject(Dictionary<ListAggregatorEntity, List<KeyValuePair<UserEntity, int>>> listAggregatinPermissionAndUser)
        {
            var listTemp = _mapper.Map(listAggregatinPermissionAndUser, typeof(Dictionary<ListAggregatorEntity, List<KeyValuePair<UserEntity, int>>>),
                typeof(Dictionary<ListAggregator, List<KeyValuePair<User, int>>>)) as Dictionary<ListAggregator, List<KeyValuePair<User, int>>>;


            var dataTransfer = new List<ListAggregationWithUsersPermission>();
            // var sss = listTemp.ToList();

            foreach (var item in listTemp)
            {

                var tempListUsers = new List<UserPermissionToListAggregation>();

                item.Value.ForEach(a => tempListUsers.Add(new UserPermissionToListAggregation { User = a.Key, Permission = a.Value }));

                dataTransfer.Add(new ListAggregationWithUsersPermission { ListAggregator = item.Key, UsersPermToListAggr = tempListUsers });

            }

            return dataTransfer;
        }
        public async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPerm2Async(string userName)
        {
            var listAggregatorsEntities = await _context.UserListAggregators

               .Include(a => a.ListAggregator).Where(a => a.User.EmailAddress == userName && a.PermissionLevel == 1)
               .Select(a => a.ListAggregator).ToListAsync();

            var listAggregators = _mapper.Map<List<ListAggregator>>(listAggregatorsEntities);

            var dataTransfer = new List<ListAggregationWithUsersPermission>();


            foreach (var listAggr in listAggregators)
            {
                var tempListAggregationForPermission = new ListAggregationWithUsersPermission();

                dataTransfer.Add(tempListAggregationForPermission);

                tempListAggregationForPermission.ListAggregator = listAggr;


                tempListAggregationForPermission.UsersPermToListAggr = new List<UserPermissionToListAggregation>();


                var tempList = await _context.UserListAggregators.AsQueryable().Where(a => a.ListAggregatorId == listAggr.ListAggregatorId)
                    .Select(a => new { UserId = a.UserId, Permission = a.PermissionLevel }).ToListAsync();


                foreach (var item in tempList)
                {
                    var tempUserEntity = await _context.Users.AsQueryable().Where(a => a.UserId == item.UserId).FirstOrDefaultAsync();

                    var tempUser = _mapper.Map<User>(tempUserEntity);


                    var tempUserPermissionToListAggregation = new UserPermissionToListAggregation();


                    tempListAggregationForPermission.UsersPermToListAggr.Add(tempUserPermissionToListAggregation);

                    tempUserPermissionToListAggregation.Permission = item.Permission;
                    tempUserPermissionToListAggregation.User = tempUser;

                }

            }

            return dataTransfer;
        }

        public async Task<List<UserListAggregator>> GetUserListAggrByUserId(int userId)
        {
            var list = await _context.UserListAggregators.AsQueryable().Where(a => a.UserId == userId).ToListAsync();
            return _mapper.Map<List<UserListAggregator>>(list);

        }

        public Task<List<string>> GetUserRolesByUserIdAsync(int userId)
        {

            var roles = _context.Users.AsQueryable().Where(a => a.UserId == userId).Include(a => a.UserRoles)
                .ThenInclude(a => a.Role).Select(a => a.UserRoles.Select(b => b.Role.RoleName).ToList()).FirstAsync();

            return roles;
        }

        public async Task<User> GetUserWithRolesAsync(int userId)
        {

            var userEntity = await _context.Users.AsQueryable().Where(a => a.UserId == userId).Include(a => a.UserRoles)
                .ThenInclude(a => a.Role).FirstAsync();

            var user = _mapper.Map<User>(userEntity);


            return user;
        }

        public async Task<User> LoginAsync(string userName, string password)
        {
            var userFD = await _context.Users.AsQueryable().Where(a => a.EmailAddress == userName && a.Password == password
                                                && (byte)a.LoginType == 1)
                .FirstOrDefaultAsync();

            if (userFD == null) return null;

            return _mapper.Map<User>(userFD);
        }

        public async Task<User> Register(string userName, string password, LoginType loginType)
        {
            if (await _context.Users.Where(a => a.EmailAddress == userName).AnyAsync())
                return null;

            var user = new UserEntity { EmailAddress = userName, Password = password, LoginType = (byte)loginType };

            UserRolesEntity userRoles = new UserRolesEntity { User = user, RoleId = 1 };

            _context.Add(userRoles);

            await _context.SaveChangesAsync();

            return _mapper.Map<User>(user);
        }

        public async Task<List<int>> GetUserIdsFromListAggrIdAsync(int listAggregationId)
        {
            return await _context.UserListAggregators.AsQueryable()
                .Where(a => a.ListAggregatorId == listAggregationId).Select(a => a.UserId).ToListAsync();
        }

        public async Task<List<ListAggregationWithUsersPermission>> GetListAggrWithUsersPerm_EmptyAsync(int userId)
        {
            var listAggregatorsEntities = await _context.UserListAggregators
               .Include(a => a.ListAggregator).Where(a => a.UserId == userId && a.PermissionLevel == 1)
               .Select(a => a.ListAggregator).ToListAsync();

            var listAggregators = _mapper.Map<List<ListAggregator>>(listAggregatorsEntities);

            return listAggregators.Select(a => new ListAggregationWithUsersPermission { ListAggregator = a }).ToList();
        }

        //public async Task<ListAggregationWithUsersPermission> GetListAggrWithUsersPermByListAggrIdAsync(ListAggregationWithUsersPermission listAggregationForPermission)
        //{ 
        //    listAggregationForPermission.UsersPermToListAggr = new List<UserPermissionToListAggregation>();


        //    var tempList = await _context.UserListAggregators.AsQueryable()
        //    .Where(a => a.ListAggregatorId == listAggregationForPermission.ListAggregator.ListAggregatorId)
        //        .Select(a => new { UserId = a.UserId, Permission = a.PermissionLevel }).ToListAsync();


        //    foreach (var item in tempList)
        //    {
        //        var tempUserEntity = await _context.Users.AsQueryable().Where(a => a.UserId == item.UserId).FirstOrDefaultAsync();

        //        var tempUser = _mapper.Map<User>(tempUserEntity);


        //        var tempUserPermissionToListAggregation = new UserPermissionToListAggregation();


        //        listAggregationForPermission.UsersPermToListAggr.Add(tempUserPermissionToListAggregation);

        //        tempUserPermissionToListAggregation.Permission = item.Permission;
        //        tempUserPermissionToListAggregation.User = tempUser;


        //    }

        //    return listAggregationForPermission;
        //}

        public async Task<List<UserPermissionToListAggregation>> GetListUsersPermissionByListAggrIdAsync(int listAggregatorId)
        {
            var listUsersPermToListAggr = new List<UserPermissionToListAggregation>();


            var tempList = await _context.UserListAggregators.AsQueryable()
            .Where(a => a.ListAggregatorId == listAggregatorId)
                .Select(a => new { UserId = a.UserId, Permission = a.PermissionLevel }).ToListAsync();


            foreach (var item in tempList)
            {
                var tempUserEntity = await _context.Users.AsQueryable().Where(a => a.UserId == item.UserId).FirstOrDefaultAsync();

                var tempUser = _mapper.Map<User>(tempUserEntity);


                var tempUserPermissionToListAggregation = new UserPermissionToListAggregation();


                listUsersPermToListAggr.Add(tempUserPermissionToListAggregation);

                tempUserPermissionToListAggregation.Permission = item.Permission;
                tempUserPermissionToListAggregation.User = tempUser;

            }

            return listUsersPermToListAggr;
        }

            

        public async Task AddRefreshToken(int userId, RefreshTokenSession refreshTokenSession)
        {
            var refreshTokenSessions = await _context.RefreshTokenSessions.Where(a => a.UserId == userId).ToListAsync(); 

            if (!refreshTokenSessions.Any())
            {
                _context.Add(refreshTokenSession.ToRefreshTokenSessionEntity());
                await _context.SaveChangesAsync();
                return;
            }

            _context.RemoveRange(refreshTokenSessions.Where(a => a.ExpiresAt < System.DateTime.Now ||
                                                                 a.DeviceInfo == refreshTokenSession.DeviceInfo));

            _context.Add(refreshTokenSession.ToRefreshTokenSessionEntity());
            await _context.SaveChangesAsync();
        }

        public Task<List<RefreshTokenSession>> GetRefreshTokens(int userId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRefreshToken(int userId, RefreshTokenSession refreshTokenSession)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRefreshTokenByJti(int userId, string jti)
        {
            throw new NotImplementedException();
        }

        public Task ReplaceRefreshToken(int userId, RefreshTokenSession oldRefreshTokenSession, RefreshTokenSession newRefreshTokenSession)
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> ReplaceRefreshToken2(int userId, string deviceId, string refreshTokenOld, string accessTokenNew, string jti, int version, string refreshTokenNew, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
