using AutoMapper;
using EFDataBase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NuGet.Protocol;
using ServiceMediatR.SignalREvents;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using Shared.DataEndpoints.Models.Requests;
using ShoppingListWebApi.Data;
using ShoppingListWebApi.Models.Requests;
using ShoppingListWebApi.Models.Response;
using ShoppingListWebApi.Token;
using SignalRService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;


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
        private readonly ITokenEndpoint _tokenEndpoint;
        private readonly ILogger<UserController> _logger;
        private readonly ITokenService _tokenService;
        private readonly IMemoryCache _memoryCache;

        public UserController(IMapper mapper, IConfiguration configuration, IMediator mediator
            , SignarRService signarRService, IUserEndpoint userEndpoint, ITokenEndpoint tokenEndpoint, ILogger<UserController> logger
            , ITokenService tokenService, IMemoryCache memoryCache)
        {
            _mapper = mapper;
            _configuration = configuration;
            _mediator = mediator;
            _signarRService = signarRService;
            _userEndpoint = userEndpoint;
            _tokenEndpoint = tokenEndpoint;
            _logger = logger;
            _tokenService = tokenService;
            _memoryCache = memoryCache;
        }


        //[HttpGet("{id}")]             // not used
        //[Authorize(Roles ="Admin")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userEndpoint.FindUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new ProblemDetails { Title = "User not found." });
            }

            return user;
        }


        //string access_token,
        //    string data_access_expiration_time,
        //    string expires_in,
        //    string long_lived_token,
        //    string state


        [HttpGet("FacebookToken")]
        public async Task<ActionResult<UserNameAndTokensResponse>> FacebookToken(string access_token, string state)
        {
            var deviceId = state.Split("=").Last();

            MeResponse meResponse = null;

            if (string.IsNullOrEmpty(access_token))
            {
                return Problem(title: "Some errors occurred.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            meResponse = await WebApiHelper.GetFacebookUserFromTokenAsync(access_token, state, _configuration);


            var user = await _userEndpoint.GetUserByNameAsync(meResponse.email);

            if (user == null)
            {

                user = await _userEndpoint.Register(meResponse.email, string.Empty, LoginType.Facebook);
                var (accessToken, refreshToken) = await GenerateToken2(user.UserId, deviceId);


                return new UserNameAndTokensResponse { Token = accessToken, RefreshToken = refreshToken, UserName = user.EmailAddress };


            }
            else
            {
                if ((LoginType)user.LoginType == LoginType.Facebook)
                {
                    var (accessToken, refreshToken) = await GenerateToken2(user.UserId, deviceId); //TODO;

                    return Ok(

                        new UserNameAndTokensResponse { Token = accessToken, RefreshToken = refreshToken, UserName = user.EmailAddress }
                        );
                }

            }

            return Conflict(new ProblemDetails { Title = "User Exist" });

        }


        [HttpGet]
        public async Task<IActionResult> FacebookCode(string code, string state)
        {
            MeResponse meResponse = null;

            string referer = Request.Headers["Referer"].ToString();
            var returnUrl = state.Split(",").Last().Split("=").Last();
            var deviceId = state.Split(",")[^2].Split("=").Last();
            string myDomain = Request.GetDisplayUrl().Split('?')[0];

            if (!string.IsNullOrEmpty(code))
                meResponse = await WebApiHelper.GetFacebookUserFromCodeAsync(code, state, _configuration, myDomain);
            else
                return BadRequest();

            var user = await _userEndpoint.GetUserByNameAsync(meResponse.email);

            if (user == null)
            {

                user = await _userEndpoint.Register(meResponse.email, string.Empty, LoginType.Facebook);
                var (accessToken, refreshToken) = await GenerateToken2(user.UserId, deviceId);

                AddRefreshToken(refreshToken);
                string id = Guid.NewGuid().ToString();
                _memoryCache.Set(id,accessToken,TimeSpan.FromSeconds(60) );
                return Redirect($"{returnUrl}/#/?id={id}");

            }
            else
            {
               

                if (user.LoginType == 2) // 2 ==>> LoginType.Facebook
                {
                    var (accessToken, refreshToken) = await GenerateToken2(user.UserId, deviceId); //TODO

                    AddRefreshToken(refreshToken);
                    string id = Guid.NewGuid().ToString();
                    _memoryCache.Set(id, accessToken, TimeSpan.FromSeconds(60));
                    return Redirect($"{returnUrl}/#/?id={id}&sss=(rrr)");
                }

            }

            return Redirect($"{returnUrl}?error=Email already exist");

        }
        [HttpGet("GetAccessToken")]
        public async Task<ActionResult<GetAccessTokenResponse>> GetAccessToken(string id)
        {
            if(_memoryCache.TryGetValue(id, out string accessToken))
            {
                return  await Task.FromResult(new GetAccessTokenResponse { AccessToken = accessToken });
            }

            return NotFound(new ProblemDetails { Title = "Token not found." } );
        }
        private void AddRefreshToken(string refreshToken)
        {
            HttpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                // Domain = "localhost",
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserNameAndTokensResponse>> Login(LoginRequest login)
        {
            _logger.LogInformation($"user controlel log in {login.UserName} ");

            var a = (byte)LoginType.Facebook;

            var user = await _userEndpoint.LoginAsync(login.UserName, login.Password);

            _logger.LogInformation($"user controlel log out ");

            if (user == null)
            {
                return Unauthorized(new ProblemDetails { Title = "Invalid username or password." });
            }

            var (accessToken, refreshToken) = await GenerateToken2(user.UserId, login.DeviceId);

            AddRefreshToken(refreshToken);

            return new UserNameAndTokensResponse
            {
                UserName = login.UserName,
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }



        [HttpPost("Register")]
        public async Task<ActionResult<string>> Register(RegistrationRequest request)
        {

            User user = null;

            try
            {
                user = await _userEndpoint.Register(request.UserName, request.Password, LoginType.Local);
                if (user == null)
                {
                    return Conflict(new ProblemDetails { Title = "User already exist." });
                }
                var (accessToken, refreshToken) = await GenerateToken2(user.UserId, request.DeviceId);

                AddRefreshToken(refreshToken);

                return Ok(new UserNameAndTokensResponse
                {
                    UserName = user.EmailAddress,
                    Token = accessToken,
                    RefreshToken = refreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {UserName}", request.UserName);

                return Problem(statusCode: 500, title: "Server error.");
            }
        }

        [HttpGet("UserDataTree")]
        [Authorize]
        //[Authorize(Roles ="User")]
        public async Task<ActionResult<User>> GetUserDataTree()
        {
            var name = User.FindFirstValue(ClaimTypes.Name);

            var userTemp = await _userEndpoint.GetTreeAsync(name);

            return Ok(userTemp);


        }

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks = new();

        [HttpGet("GetNewToken")]
        [Authorize(AuthenticationSchemes = "NoLifetimeBearer")]
        public async Task<ActionResult<UserNameAndTokensResponse>> GetNewToken(CancellationToken cancellationToken)
        {

            var userName = User.FindFirstValue(ClaimTypes.Name);
            var sem = _userLocks.GetOrAdd(userName, _ => new SemaphoreSlim(1, 1));

            await sem.WaitAsync(TimeSpan.FromSeconds(5));
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var version = int.Parse(User.FindFirstValue(ClaimTypes.Version));
            try
            {
                var refreshToken = HttpContext.Request.Headers["refresh_token"].ToString();

                var isCookie = false;
                if (HttpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshTokenFromCookie))
                {
                    refreshToken = refreshTokenFromCookie;
                    isCookie = true;
                }

                var deviceId = HttpContext.Request.Headers["deviceid"].ToString();

                //var lenght = deviceId.Length;

                //if (deviceId.Length == 36)
                //{

                //}

                // var (newAccessToken, newrefreshToken) = await _tokenService.RefreshTokensAsync(id, refreshToken);
                var (newAccessToken, newrefreshToken) = await _tokenService.RefreshTokensAsync2(id, deviceId, refreshToken, version, cancellationToken);

                if (string.IsNullOrEmpty(newAccessToken) || string.IsNullOrEmpty(newrefreshToken))
                {
                    return Unauthorized(new ProblemDetails { Title = "Invalid token." });

                }

                if (isCookie)
                {
                    HttpContext.Response.Cookies.Append("refreshToken", newrefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        // Domain = "localhost",
                        Expires = DateTimeOffset.UtcNow.AddDays(30)
                    });
                }
                return new UserNameAndTokensResponse
                {
                    UserName = userName,
                    Token = newAccessToken,
                    RefreshToken = isCookie ? string.Empty : newrefreshToken
                }; 
            }
            finally
            {
                sem.Release();
            }


        }

        private async Task<(string accessToken, string refreshToken)> GenerateToken2(int userId, string deviceId)
        {
            var (accessToken, refreshToken) = await _tokenService.GenerateTokens(userId, deviceId);

            return (accessToken, refreshToken);
        }


        [HttpGet("VerifyToken2")]
        [Authorize(AuthenticationSchemes = "ClockSkewZero")]
        public ActionResult VerifyToken2()
        {
            //var a = new JwtSecurityTokenHandler();
            //var b = a.ReadToken("");
            //b.
            return Ok();
        }

        [HttpGet("VerifyToken")]
        public bool GetUserFromAccessTokenAsync(string accessToken)
        {
            var userName = User.FindFirstValue(ClaimTypes.Name);

            return _tokenService.VerifyToken(accessToken);
        }

        [HttpPost("VerifyAcceessRefreshTokens")]
        [Authorize(AuthenticationSchemes = "NoLifetimeBearer")]
        public async Task<ActionResult> VerifyAcceessRefreshTokens(VerifyAccessRefreshTokenRequest request)
        {
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (await _tokenService.VerifyAcceessRefreshTokens(id, jti, request))
            {
                return Ok();
            }
            return Forbid(); // new ObjectResult(new ProblemDetails { Title = message.Message }) { StatusCode = 403 },();
        }

        [HttpGet("LogOut")]
        [Authorize]
        public async Task<ActionResult> LogOut()
        {
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _tokenEndpoint.DeleteRefreshTokenByJti(id, jti);

            return Ok();
        }

    }
}