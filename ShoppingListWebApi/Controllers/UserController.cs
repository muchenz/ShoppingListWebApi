using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ServiceMediatR.SignalREvents;
using Shared;
using ShoppingListWebApi.Data;
using SignalRService;


namespace ShoppingListWebApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ShopingListDBContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly SignarRService _signarRService;
        private readonly IUserEndpoint _userEndpoint;

        public UserController(ShopingListDBContext context, IMapper mapper, IConfiguration configuration, IMediator mediator
            , SignarRService signarRService, IUserEndpoint userEndpoint)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
            _mediator = mediator;
            _signarRService = signarRService;
            _userEndpoint = userEndpoint;
        }


        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<MessageAndStatus>> GetUser(int id)
        {
            var user = await _userEndpoint.FindUserByIdAsync(id);

            if (user == null)
            {
                return new MessageAndStatus { Status = "ERROR", Message = "User not found." };
            }

            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(user) };
        }


        //string access_token,
        //    string data_access_expiration_time,
        //    string expires_in,
        //    string long_lived_token,
        //    string state


        [HttpGet("FacebookToken")]
        public async Task<MessageAndStatusAndData<TokenAndEmailData>> FacebookToken(string access_token, string state)
        {
            MeResponse meResponse = null;

            if (!string.IsNullOrEmpty(access_token))
                meResponse = await WebApiHelper.GetFacebookUserFromTokenAsync(access_token, state, _configuration);
            else
                return new MessageAndStatusAndData<TokenAndEmailData>(null, "Some Errors", true);

            var user = await _userEndpoint.GetUserByNameAsync(meResponse.email);

            if (user == null)
            {

                var res = await Register(meResponse.email, "", LoginType.Facebook);

                return new MessageAndStatusAndData<TokenAndEmailData>(

                       new TokenAndEmailData { Token = res.Message, Email = user.EmailAddress }
                       , "", false);

            }
            else
            {
                if (user.LoginType == 2)
                {
                    var token = await GenerateToken2(user.UserId);
                    
                    return new MessageAndStatusAndData<TokenAndEmailData>(
                        
                        new TokenAndEmailData {Token=token, Email=user.EmailAddress }
                        , null, false);

                }

            }

            return new MessageAndStatusAndData<TokenAndEmailData>(null, "User Exist", true);

        }


        [HttpGet]
        public async Task<IActionResult> FacebookCode(string code, string state)
        {
            MeResponse meResponse = null;

            string referer = Request.Headers["Referer"].ToString();

            string myDomain = Request.GetDisplayUrl().Split('?')[0];

            if (!string.IsNullOrEmpty(code))
                meResponse = await WebApiHelper.GetFacebookUserFromCodeAsync(code, state, _configuration, myDomain);
            else
                return BadRequest();

            var user = await _userEndpoint.GetUserByNameAsync(meResponse.email);

            if (user == null)
            {

                var res = await Register(meResponse.email, "", LoginType.Facebook);


                return Redirect($"{referer}login?token={res.Message}");

            }
            else
            {
                if (user.LoginType == 2) // 2 ==>> LoginType.Facebook
                {
                    var token = await GenerateToken2(user.UserId);
                    return Redirect($"{referer}login?token={token}&sss=(rrr)");

                }

            }

            return Redirect($"{referer}login?error=Email already exist");

        }

        [HttpPost("Login")]
        public async Task<MessageAndStatus> Login(string userName, string password)
        {
            var a = (byte)LoginType.Facebook;

            var user = await _userEndpoint.LoginAsync(userName, password);


            if (user == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User" };
            else
                return
                    new MessageAndStatus { Status = "OK", Message = await GenerateToken2(user.UserId) };
            // Ok(await GenerateAccessTokenAsync(user.UserId));
        }


        [HttpPost("Register")]
        public async Task<MessageAndStatus> Register(string userName, string password, LoginType loginType = LoginType.Local)
        {

            User user=null;

            try
            {
                user = await _userEndpoint.Register(userName, password, loginType);
            }
            catch (Exception ex)
            {

                return new MessageAndStatus { Status = "ERROR", Message = "" };
            }

            return new MessageAndStatus { Status = "OK", Message = await GenerateToken2(user.UserId) };
            // Ok(await GenerateAccessTokenAsync(user.UserId));
        }

        [HttpPost("GetUserDataTree")]
        [Authorize]
        //[Authorize(Roles ="User")]
        public async Task<ActionResult<MessageAndStatus>> GetUserDataTree(string userName)
        {

            var userTemp = await _userEndpoint.GetTreeAsync(userName);

            // var token = await GenerateAccessTokenAsync(id);

            // var userrr = GetUserFromAccessToken(token);

            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(userTemp) };


        }


        [HttpPost("AddUserPermission")]
        [SecurityLevel(1)]
        public async Task<ActionResult<MessageAndStatus>> AddUserPermission(int listAggregationId, [FromBody] UserPermissionToListAggregation item)
        {

            var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

            if (user == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User not exist." };

            var isUserHasListAgregation = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);
            
            if (isUserHasListAgregation)
                return new MessageAndStatus { Status = "ERROR", Message = "User is on list" };


            await _userEndpoint.AddUserListAggregationAsync(user.UserId, listAggregationId, item.Permission);

            return new MessageAndStatus { Status = "OK", Message = "User was added." };
        }


        [HttpPost("InviteUserPermission")]
        [SecurityLevel(2)]
        public async Task<ActionResult<MessageAndStatus>> InviteUserPermission(int listAggregationId, 
            [FromBody] UserPermissionToListAggregation item, [FromHeader]string signalRId)
        {

            var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

            if (user == null)
                return await Task.FromResult(new MessageAndStatus { Status = "ERROR", Message = "User not exist." });

            var IsUserInvitatedToListAggregation = await _userEndpoint.IsUserInvitatedToListAggregationAsync(item.User.EmailAddress, listAggregationId);

            if (IsUserInvitatedToListAggregation)
                return await Task.FromResult(new MessageAndStatus { Status = "ERROR", Message = "Ivitation is on list" });

            //bbbb = _context.UserListAggregators.AsQueryable().Where(a => a.UserId == user.UserId && a.ListAggregatorId == listAggregationId).Any();
           
            var isUserHasListAgregation = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);
           
            if (isUserHasListAgregation)
                return await Task.FromResult(new MessageAndStatus { Status = "ERROR", Message = "User already has permission." });

            var senderName = HttpContext.User.Identity.Name;

            await _userEndpoint.AddInvitationAsync(item.User.EmailAddress, listAggregationId, item.Permission, senderName);


            await _signarRService.SendRefreshMessageToUsersAsync(new List<int> { user.UserId}, "New_Invitation",signalRId: signalRId);

            return await Task.FromResult(new MessageAndStatus { Status = "OK", Message = "Ivitation was added." });
        }





        [HttpPost("ChangeUserPermission")]
        [SecurityLevel(1)]
        public async Task<ActionResult<MessageAndStatus>> ChangeUserPermission(int listAggregationId
            , [FromBody] UserPermissionToListAggregation item, [FromHeader]string signalRId)
        {

            var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

            if (user == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User not exist." };


            var count = await _userEndpoint.GetNumberOfAdministratorsOfListAggregationsAsync(listAggregationId);

            int lastAdminId = -1;
            if (count == 1)
                lastAdminId = await _userEndpoint.GetLastAdminIdAsync(listAggregationId);

            if (count == 1 && user.UserId == lastAdminId)
                return new MessageAndStatus { Status = "ERROR", Message = "Only one Admin left - not changed." };


            //var userListAggr = await _context.UserListAggregators.AsQueryable().Where(a => a.User.UserId == item.User.UserId && a.ListAggregatorId == listAggregationId)
            //    .FirstOrDefaultAsync();

            //if (userListAggr == null)
            //    return new MessageAndStatus { Status = "ERROR", Message = "User permission not found." };
            //else
            //    userListAggr.PermissionLevel = item.Permission;


            ////  _context.Update(userListAggr);
            //await _context.SaveChangesAsync();

            bool isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(user.UserId, listAggregationId);


            if (!isUserHasListAggregator)
                return new MessageAndStatus { Status = "ERROR", Message = "User permission not found." };
            else
                await _userEndpoint.SetUserPermissionToListAggrAsync(user.UserId, listAggregationId, item.Permission);

            await _mediator.Publish(new DataChangedEvent(new int[] { user.UserId }, signalRId));

            return new MessageAndStatus { Status = "OK", Message = "Permission has changed." };
        }

        [HttpPost("DeleteUserPermission")]
        [SecurityLevel(1)]
        public async Task<ActionResult<MessageAndStatus>> DeleteUserPermission(int listAggregationId
            , [FromBody] UserPermissionToListAggregation item, [FromHeader]string signalRId)
        {

            var user = await _userEndpoint.GetUserByNameAsync(item.User.EmailAddress);

            if (user == null)
                return new MessageAndStatus { Status = "ERROR", Message = "User not exist." };


            var count = await _userEndpoint.GetNumberOfAdministratorsOfListAggregationsAsync(listAggregationId);

            int lastAdminId = -1;
            if (count == 1)
                lastAdminId = await _userEndpoint.GetLastAdminIdAsync(listAggregationId);

            if (count == 1 && user.UserId == lastAdminId)
                return new MessageAndStatus { Status = "ERROR", Message = "Only one Admin left - not delete." };


            var isUserHasListAggregator = await _userEndpoint.IsUserHasListAggregatorAsync(item.User.UserId, listAggregationId);

            if (!isUserHasListAggregator)
                return new MessageAndStatus { Status = "ERROR", Message = "User permission not found." };
            else
                await _userEndpoint.DeleteUserListAggrAscync(item.User.UserId, listAggregationId);


            await _mediator.Publish(new DataChangedEvent(new int[] { user.UserId }, signalRId));

            return new MessageAndStatus { Status = "OK", Message = "User permission was deleted." };
        }
     

        [Authorize]
        [HttpPost("GetListAggregationForPermission")]
        public async Task<ActionResult<MessageAndStatus>> GetListAggregationForPermission(string userName)
        {
          
            var dataTransfer = await _userEndpoint.GetListAggregationForPermission2Async(userName);


            return new MessageAndStatus { Status = "OK", Message = JsonConvert.SerializeObject(dataTransfer) };

        }

        [Authorize]
        [HttpPost("GetListAggregationForPermission_Empty")]
        public async Task<ActionResult<MessageAndStatusAndData<List<ListAggregationForPermission>>>> GetListAggregationForPermission_Empty()
        {

            var sUnerId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;

            var userId = int.Parse(sUnerId);

            var dataTransfer = await _userEndpoint.GetListAggregationForPermission_EmptyAsync(userId);

            
            return new MessageAndStatusAndData<List<ListAggregationForPermission>>(dataTransfer, "OK", false);

        }

        [Authorize]
        [HttpPost("GetListAggregationForPermissionByListAggrId")]
        public async Task<ActionResult<MessageAndStatusAndData<ListAggregationForPermission>>> 
            GetListAggregationForPermissionByListAggrId([FromBody]ListAggregationForPermission listAggregationForPermission)
        {

            var data = await _userEndpoint.GetListAggregationForPermissionByListAggrIdAsync(listAggregationForPermission);
            
            return new MessageAndStatusAndData<ListAggregationForPermission>(data, "OK", false);

        }
        private async Task<string> GenerateAccessTokenAsync(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);


            var roles = await GetUserRolesByUserIdAsync(userId);
            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.Name, Convert.ToString(userId)));

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));

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

            var userListAggregators = await _userEndpoint.GetUserListAggrByUserId(userId);

            userListAggregators.ForEach(item => claims.Add(new Claim("ListAggregator", $"{item.ListAggregatorId}.{item.PermissionLevel}")));

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


        Task<List<string>> GetUserRolesByUserIdAsync(int userId)
        {

           
            return _userEndpoint.GetUserRolesByUserIdAsync(userId);
        }

        Task<User> GetUserWithRolesAsync(int userId)
        {
            return _userEndpoint.GetUserWithRolesAsync(userId);
        }

        [HttpGet("VerifyToken")]
        public bool GetUserFromAccessTokenAsync(string accessToken)
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
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
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