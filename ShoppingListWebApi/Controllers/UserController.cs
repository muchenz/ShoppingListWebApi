using System;
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
using NuGet.Protocol;
using ServiceMediatR.SignalREvents;
using Shared.DataEndpoints.Abstaractions;
using Shared.DataEndpoints.Models;
using ShoppingListWebApi.Data;
using ShoppingListWebApi.Models.Requests;
using ShoppingListWebApi.Models.Response;
using ShoppingListWebApi.Token;
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
        private readonly ITokenService _tokenService;

        public UserController(IMapper mapper, IConfiguration configuration, IMediator mediator
            , SignarRService signarRService, IUserEndpoint userEndpoint, ILogger<UserController> logger
            , ITokenService tokenService )
        {
            _mapper = mapper;
            _configuration = configuration;
            _mediator = mediator;
            _signarRService = signarRService;
            _userEndpoint = userEndpoint;
            _logger = logger;
            _tokenService = tokenService;
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
        public async Task<ActionResult<TokenAndEmailData>> FacebookToken(string access_token, string state)
        {
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
                var token = await GenerateToken2(user.UserId);

                return new TokenAndEmailData { Token = token, Email = user.EmailAddress };


            }
            else
            {
                if ((LoginType)user.LoginType == LoginType.Facebook)
                {
                    var token = await GenerateToken2(user.UserId);

                    return Ok(

                        new TokenAndEmailData { Token = token, Email = user.EmailAddress }
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

            string myDomain = Request.GetDisplayUrl().Split('?')[0];

            if (!string.IsNullOrEmpty(code))
                meResponse = await WebApiHelper.GetFacebookUserFromCodeAsync(code, state, _configuration, myDomain);
            else
                return BadRequest();

            var user = await _userEndpoint.GetUserByNameAsync(meResponse.email);

            if (user == null)
            {

                user = await _userEndpoint.Register(meResponse.email, string.Empty, LoginType.Facebook);
                var token = await GenerateToken2(user.UserId);

                return Redirect($"{returnUrl}/#/?token={token}");

            }
            else
            {
                if (user.LoginType == 2) // 2 ==>> LoginType.Facebook
                {
                    var token = await GenerateToken2(user.UserId);
                    return Redirect($"{returnUrl}/#/?token={token}&sss=(rrr)");
                }

            }

            return Redirect($"{returnUrl}?error=Email already exist");

        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserNameAndTokenResponse>> Login(LoginRequest login)
        {
            _logger.LogInformation($"user controlel log in {login.UserName} ");

            var a = (byte)LoginType.Facebook;

            var user = await _userEndpoint.LoginAsync(login.UserName, login.Password);

            _logger.LogInformation($"user controlel log out ");

            if (user == null)
            {
                return Unauthorized(new ProblemDetails { Title = "Invalid username or password." });
            }

            return new UserNameAndTokenResponse
            {
                UserName = login.UserName,
                Token = await GenerateToken2(user.UserId)
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

                return Ok(await GenerateToken2(user.UserId));
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

        [HttpGet("GetNewToken")]
        [Authorize(AuthenticationSchemes = "NoLifetimeBearer")]
        public async Task<ActionResult<User>> GetNewToken()
        {
            var name = User.FindFirstValue(ClaimTypes.Name);


            return Ok(name);


        }

        private  Task<string> GenerateToken2(int userId)
        {
            return _tokenService.GenerateToken(userId);
        }


        [HttpGet("VerifyToken2")]
        [Authorize]
        public ActionResult VerifyToken2()
        {
            var a = new JwtSecurityTokenHandler();
            var b = a.ReadToken("");
            b.
            return Ok();
        }

        [HttpGet("VerifyToken")]
        public bool GetUserFromAccessTokenAsync(string accessToken)
        {
           return _tokenService.VerifyToken(accessToken);
        }


    }
}