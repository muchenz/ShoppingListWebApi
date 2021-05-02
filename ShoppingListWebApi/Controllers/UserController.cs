using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Shared;
using ShoppingListWebApi.Data;


namespace ShoppingListWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly SignarRService _signarRService;

        public UserController(ShopingListDBContext context, IMapper mapper, IConfiguration configuration, SignarRService signarRService)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _signarRService = signarRService;
        }


        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<MessageAndStatus>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return new MessageAndStatus { Status = "ERROR", Message = "User not found." };
            }

            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(user) }; 
        }

        [HttpPost("Login")]
        public async Task<MessageAndStatus> Login(string userName, string password)
        {
            
            var user = await _context.Users.Where(a => a.EmailAddress == userName && a.Password == password).FirstOrDefaultAsync();
            

            if (user == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User" };
            else
                return
                    new MessageAndStatus { Status = "OK", Message = await GenerateToken2(user.UserId) }; 
                   // Ok(await GenerateAccessTokenAsync(user.UserId));
        }


        [HttpPost("Register")]
        public async Task<MessageAndStatus> Register(string userName, string password)
        {
            
            var user = new UserEntity { EmailAddress = userName, Password = password   };

            UserRolesEntity userRoles = new UserRolesEntity { User = user, RoleId = 1 };

            _context.Add(userRoles);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
             {

                return new MessageAndStatus { Status = "ERROR", Message ="" };
            }

            return new MessageAndStatus { Status = "OK", Message = await GenerateToken2(user.UserId) }; 
            // Ok(await GenerateAccessTokenAsync(user.UserId));
        }

        //[Authorize(Roles ="User")]
        //[Authorize]
        [HttpPost("GetUserDataTree")]
        [Authorize]
        public async Task<ActionResult<MessageAndStatus>> GetUserDataTree(string  userName)
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

           
            // var token = await GenerateAccessTokenAsync(id);

           // var userrr = GetUserFromAccessToken(token);

            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(userTemp) };


        }


        [HttpPost("AddUserPermission")]
        [SecurityLevel(1)]
        public async Task<ActionResult<MessageAndStatus>> AddUserPermission(int listAggregationId, [FromBody]UserPermissionToListAggregation item)
        {            

            var user = await _context.Users.Where(a => a.EmailAddress == item.User.EmailAddress).FirstOrDefaultAsync();

            if (user==null)
                return new MessageAndStatus { Status = "ERROR", Message = "User not exist." }; 

            var bbbb = _context.UserListAggregators.Where(a => a.User.UserId == item.User.UserId && a.ListAggregatorId == listAggregationId).Any();

            if (bbbb)
                return  new MessageAndStatus { Status = "ERROR", Message = "User is on list" };


            var userListAggregatorEntity = new UserListAggregatorEntity { UserId = user.UserId, ListAggregatorId = listAggregationId,
                PermissionLevel = item.Permission };


            _context.UserListAggregators.Add(userListAggregatorEntity);

            await _context.SaveChangesAsync();
                                  

            return new MessageAndStatus { Status = "OK", Message = "User was added." }; 
        }


        [HttpPost("InviteUserPermission")]
        [SecurityLevel(2)]
        public async Task<ActionResult<MessageAndStatus>> InviteUserPermission(int listAggregationId, [FromBody]UserPermissionToListAggregation item)
        {

            var user = await _context.Users.Where(a => a.EmailAddress == item.User.EmailAddress).FirstOrDefaultAsync();

            if (user == null)
                return await Task.FromResult(new MessageAndStatus { Status = "ERROR", Message = "User not exist." });

            var bbbb = _context.Invitations.Where(a => a.EmailAddress == item.User.EmailAddress && a.ListAggregatorId == listAggregationId).Any();

            if (bbbb)
                return await Task.FromResult(new MessageAndStatus { Status = "ERROR", Message = "Ivitation is on list" });

            bbbb = _context.UserListAggregators.Where(a => a.UserId == user.UserId && a.ListAggregatorId == listAggregationId).Any();

            if (bbbb)
                return await Task.FromResult(new MessageAndStatus { Status = "ERROR", Message = "User already has permission." });
            
            var senderName = HttpContext.User.Identity.Name;

            var invitationEntity = new InvitationEntity
            {
                 EmailAddress = item.User.EmailAddress,
                ListAggregatorId = listAggregationId,
                PermissionLevel = item.Permission,
                SenderName = senderName
            };


            _context.Add(invitationEntity);

            await _context.SaveChangesAsync();


            return await Task.FromResult(new MessageAndStatus { Status = "OK", Message = "Ivitation was added." });
        }

   



    [HttpPost("ChangeUserPermission")]
        [SecurityLevel(1)]
        public async Task<ActionResult<MessageAndStatus>> ChangeUserPermission(int listAggregationId, [FromBody]UserPermissionToListAggregation item)
        {

            var user = await _context.Users.Where(a => a.EmailAddress == item.User.EmailAddress).FirstOrDefaultAsync();

            if (user == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User not exist." }; 


            var count = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggregationId && a.PermissionLevel == 1).CountAsync();

            UserListAggregatorEntity lastAdmin=null;
            if (count == 1)
                lastAdmin = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggregationId && a.PermissionLevel == 1).SingleAsync();

            if (count == 1 && user.UserId== lastAdmin.UserId)
                return new MessageAndStatus { Status = "ERROR", Message = "Only one Admin left - not changed." };


            var userListAggr = await _context.UserListAggregators.Where(a => a.User.UserId == item.User.UserId && a.ListAggregatorId == listAggregationId)
                .FirstOrDefaultAsync();

            if (userListAggr == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User permission not found." }; 
            else
                userListAggr.PermissionLevel = item.Permission;


          //  _context.Update(userListAggr);
            await _context.SaveChangesAsync();

           
            await _signarRService.SendRefreshMessageToUsersAsync(new int[] { user.UserId });

            return new MessageAndStatus { Status = "OK", Message = "Permission has changed." }; 
        }

        [HttpPost("DeleteUserPermission")]
        [SecurityLevel(1)]
        public async Task<ActionResult<MessageAndStatus>> DeleteUserPermission(int listAggregationId, [FromBody]UserPermissionToListAggregation item)
        {

            var user = await _context.Users.Where(a => a.EmailAddress == item.User.EmailAddress).FirstOrDefaultAsync();

            if (user == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User not exist." }; 


            var count = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggregationId && a.PermissionLevel == 1).CountAsync();

            UserListAggregatorEntity lastAdmin = null;
            if (count == 1)
                lastAdmin = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggregationId && a.PermissionLevel == 1).SingleAsync();

            if (count == 1 && user.UserId == lastAdmin.UserId)
                return new MessageAndStatus { Status = "ERROR", Message = "Only one Admin left - not delete." };


            var userListAggr = await _context.UserListAggregators.Where(a => a.User.UserId == item.User.UserId && a.ListAggregatorId == listAggregationId)
                .FirstOrDefaultAsync();
            
            if (userListAggr==null)
                return new MessageAndStatus { Status = "ERROR", Message = "User permission not found." }; 
            else            
                _context.Remove(userListAggr);
                       

            await _context.SaveChangesAsync();

            await _signarRService.SendRefreshMessageToUsersAsync(new int[] { user.UserId });

            return new MessageAndStatus { Status = "OK", Message = "User permission was deleted." }; 
        }
        public class ListAggregationForPermission
        {

            public ListAggregator ListAggregatorEntity { get; set; }

            public List<UserPermissionToListAggregation> Users { get; set; }
        }

        public class UserPermissionToListAggregation
        {

            public User User { get; set; }
            public int Permission { get; set; }

        }

        [Authorize]
        [HttpPost("GetListAggregationForPermission")]
        public async Task<ActionResult<MessageAndStatus>> GetListAggregationForPermission(string userName)
        {
            var userListAggregatorsEntities = await _context.UserListAggregators

                .Include(a => a.ListAggregator).Where(a => a.User.EmailAddress == userName && a.PermissionLevel == 1)
                .Select(a => a.ListAggregator).ToListAsync();


            var listAggregatinPermissionAndUser = new Dictionary<ListAggregatorEntity, List<KeyValuePair<UserEntity, int>>>();


            foreach (var item in userListAggregatorsEntities)
            {

                var user = _context.UserListAggregators.Where(a => a.ListAggregatorId == item.ListAggregatorId)
                    .Select(a => new KeyValuePair<UserEntity, int>(a.User, a.PermissionLevel));

                var userList = await user.ToListAsync();

                listAggregatinPermissionAndUser.Add(item, userList);
            }

            List<ListAggregationForPermission> dataTransfer = ConvertFromEntitiesToDTOtListObject(listAggregatinPermissionAndUser);


            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(dataTransfer) }; 


        }

        private List<ListAggregationForPermission> ConvertFromEntitiesToDTOtListObject(Dictionary<ListAggregatorEntity, List<KeyValuePair<UserEntity, int>>> listAggregatinPermissionAndUser)
        {
            var listTemp = _mapper.Map(listAggregatinPermissionAndUser, typeof(Dictionary<ListAggregatorEntity, List<KeyValuePair<UserEntity, int>>>),
                typeof(Dictionary<ListAggregator, List<KeyValuePair<User, int>>>)) as Dictionary<ListAggregator, List<KeyValuePair<User, int>>>;


            var dataTransfer = new List<ListAggregationForPermission>();
            // var sss = listTemp.ToList();

            foreach (var item in listTemp)
            {

                var tempListUsers = new List<UserPermissionToListAggregation>();

                item.Value.ForEach(a => tempListUsers.Add(new UserPermissionToListAggregation { User = a.Key, Permission = a.Value }));

                dataTransfer.Add(new ListAggregationForPermission { ListAggregatorEntity = item.Key, Users = tempListUsers });

            }

            return dataTransfer;
        }

        private async Task<string> GenerateAccessTokenAsync(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);
                       

            var roles = await GetUserRoles(userId);
            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Name, Convert.ToString(userId)));

            foreach (var item in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, item.RoleName));

            }


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(10),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
               
            };


            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        async Task<string> GenerateToken2(int userId)
        {
            var user = await GetUserWithRolesAsync(userId);

            var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name,user.EmailAddress),
            new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddDays(1000)).ToUnixTimeSeconds().ToString()),

            };

            //var roles = await GetUserRoles(userId);

           

            user.Roles.ToList().ForEach(role => claims.Add(new Claim(ClaimTypes.Role, role)));

            var userListAggregators = await _context.UserListAggregators.Where(a => a.UserId == userId).ToListAsync();
            userListAggregators.ForEach(item => claims.Add(new Claim("ListAggregator",$"{item.ListAggregatorId}.{item.PermissionLevel}")));

            var token = new JwtSecurityToken(
                new JwtHeader(
                    new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"])),
                    SecurityAlgorithms.HmacSha256)),
                new JwtPayload(claims)
                );
            string stringToken = "";
            try
            {
                stringToken = new JwtSecurityTokenHandler().WriteToken(token);
            }

            catch (Exception ex)
            {


            }
                        

            return stringToken;
        }


        async Task<List<RoleEntity>> GetUserRoles(int userId)
        {

            var roles = await _context.Users.Where(a => a.UserId == userId).Include(a=>a.UserRoles)
                .ThenInclude(a=>a.Role).Select(a=>a.UserRoles.Select(b=>b.Role).ToList()).FirstAsync();

            return roles;
        }

        async Task<User> GetUserWithRolesAsync(int userId)
        {

            var userEntity = await _context.Users.Where(a => a.UserId == userId).Include(a => a.UserRoles)
                .ThenInclude(a => a.Role).FirstAsync();

            var user = _mapper.Map<User>(userEntity);


            return user;
        }

        private User GetUserFromAccessToken(string accessToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };

                SecurityToken securityToken;
                var principle = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out securityToken);

                JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken != null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    var userId = principle.FindFirst(ClaimTypes.Name)?.Value;

                    var userEnity = _context.Users.Include(u => u.UserRoles).ThenInclude(a => a.Role)
                                        .Where(u => u.UserId == Convert.ToInt32(userId)).FirstOrDefault();

                    return _mapper.Map<User>(userEnity);
                }
            }
            catch (Exception)
            {
                return new User();
            }

            return new User();
        }





        private static User MapUserEntityToUser(UserEntity userDTO)
        {
            var user = new User
            {
                EmailAddress = userDTO.EmailAddress,
                UserId = userDTO.UserId
            };

            foreach (var agr in userDTO.UserListAggregators)
            {

                var la = new ListAggregator
                {

                    ListAggregatorName = agr.ListAggregator.ListAggregatorName,
                    ListAggregatorId = agr.ListAggregator.ListAggregatorId,
                };

                user.ListAggregators.Add(la);


                foreach (var list in agr.ListAggregator.Lists)
                {
                    var li = new List
                    {
                        ListId = list.ListId,
                        ListName = list.ListName

                    };

                    la.Lists.Add(li);

                    foreach (var listItem in list.ListItems)
                    {
                        var litem = new ListItem
                        {
                            ListItemId = listItem.ListItemId,
                            ListItemName = listItem.ListItemName
                        };

                        li.ListItems.Add(litem);
                    }

                }

            }

            return user;
        }
    }
}