using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ServiceMediatR.SignalREvents;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using ShoppingListWebApi.Data;
using ShoppingListWebApi.Models.Response;
using SignalRService;


namespace ShoppingListWebApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly SignarRService _signarRService;
        private readonly IUserEndpoint _userEndpoint;
        private readonly ILogger<UserController> _logger;

        public UserController(IMapper mapper, IConfiguration configuration, IMediator mediator
            , SignarRService signarRService, IUserEndpoint userEndpoint, ILogger<UserController> logger)
        {
            _mapper = mapper;
            _configuration = configuration;
            _mediator = mediator;
            _signarRService = signarRService;
            _userEndpoint = userEndpoint;
            _logger = logger;
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
                return MessageAndStatusAndData<TokenAndEmailData>.Fail("Some Errors");

            var user = await _userEndpoint.GetUserByNameAsync(meResponse.email);

            if (user == null)
            {

                var res = await Register(meResponse.email, "", LoginType.Facebook);

                return MessageAndStatusAndData<TokenAndEmailData>.Ok(

                       new TokenAndEmailData { Token = res.Message, Email = user.EmailAddress }
                       );

            }
            else
            {
                if (user.LoginType == 2)
                {
                    var token = await GenerateToken2(user.UserId);
                    
                    return MessageAndStatusAndData<TokenAndEmailData>.Ok(
                        
                        new TokenAndEmailData {Token=token, Email=user.EmailAddress }
                        );

                }

            }

            return MessageAndStatusAndData<TokenAndEmailData>.Fail("User Exist");

        }


        [HttpGet]
        public async Task<IActionResult> FacebookCode(string code, string state)
        {
            MeResponse meResponse = null;

            string referer = Request.Headers["Referer"].ToString();
            var returnUrl = state.Split(",").Last().Split("=").Last();

            string myDomain = Request.GetDisplayUrl().Split('?')[0];

            if (!string.IsNullOrEmpty(code))
                meResponse = await WebApiHelper.GetFacebookUserFromCodeAsync(code, state, _configuration, myDomain);
            else
                return BadRequest();

            var user = await _userEndpoint.GetUserByNameAsync(meResponse.email);

            if (user == null)
            {

                var res = await Register(meResponse.email, "", LoginType.Facebook);


                return Redirect($"{returnUrl}?token={res.Message}");

            }
            else
            {
                if (user.LoginType == 2) // 2 ==>> LoginType.Facebook
                {
                    var token = await GenerateToken2(user.UserId);
                    return Redirect($"{returnUrl}?token={token}&sss=(rrr)");

                }

            }

            return Redirect($"{returnUrl}?error=Email already exist");

        }

        [HttpPost("Login")]
        public async Task<MessageAndStatus> Login(string userName, string password)
        {
            _logger.LogInformation($"user controlel log in {userName} {password} ");

            var a = (byte)LoginType.Facebook;

            var user = await _userEndpoint.LoginAsync(userName, password);

            _logger.LogInformation($"user controlel log out ");

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


      
        private async Task<string> GenerateAccessTokenAsync(int userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);


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
                    new SigningCredentials(
                        //new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"])),
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes("eashfisahfihgiuashrilghas9ifhiuhvi9uashblvh938hen48239")),
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
                Console.WriteLine(ex.Message);

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
                //var key = Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"]);
                var key = Encoding.UTF8.GetBytes("eashfisahfihgiuashrilghas9ifhiuhvi9uashblvh938hen48239");

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


      
    }
}