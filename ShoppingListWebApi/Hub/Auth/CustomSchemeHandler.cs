using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Hub.Auth
{
    public class CustomSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;
        public const string CustomScheme = nameof(CustomScheme);

        public CustomSchemeHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
            , AuthService authService
            , IConfiguration configuration
        ) : base(options, logger, encoder, clock)
        {
            _authService = authService;
            _configuration = configuration;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                //goto god;
                var isToken = Request.Headers.ContainsKey("Access_Token");

                if (!isToken)
                    return await Task.FromResult(AuthenticateResult.Fail("Not authorized. Lack Access_Token."));

                var accessToken = Request.Headers["Access_Token"].ToString();

                var isTokenGood = await _authService.IsValidateTokenAsync(accessToken);
                //var isTokenGood = await _authService.IsValidateTokenAsync(accessToken);

                if (!isTokenGood)
                    return await Task.FromResult(AuthenticateResult.Fail("Not authorized.  Access_Token is wrong."));


                // pozyskać nameidentifier (user ID)
                // ustawić claim  nameidentifier na UserID
                // to to żeby SignaR wukoszytsał to w Clients.User(item.ToString()).SendAsync("DataAreChanged_"+item)

                //var handler = new JwtSecurityTokenHandler();
                //var jwtToken = handler.ReadJwtToken(accessToken);

                Claim[] claims = new Claim[] { };


                if (!_authService.IsGodToken(accessToken))
                {
                    var (userIdClaim, userNameClaim) = GetClaimFromTokenWithoutVerification(accessToken);

                    //god:
                    claims = [ userIdClaim,
                            userNameClaim];
                }
                else
                {
                    claims = [new Claim(ClaimTypes.Name, "Sever"), 
                              new Claim(ClaimTypes.NameIdentifier,"-1")  ];
                }

                    var identity = new ClaimsIdentity(claims, CustomScheme);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, new AuthenticationProperties()
                    , CustomScheme);

                return await Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                Console.WriteLine("--------------");
                Console.WriteLine(ex);
                Console.WriteLine("--------------");


                return await Task.FromResult(AuthenticateResult.Fail("Not authorized.  Someting go wrong."));
            }
        }

        (Claim NameIdentifier, Claim Name) GetClaimFromTokenWithoutVerification(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);


            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            return (userId, name);
        }


        (Claim NameIdentifier, Claim Name) GetClaimFromTokenWithVerificationByKey(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("Secrets")["JWTSecurityKey"])),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(accessToken, validationParameters, out SecurityToken validatedToken);

            var name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            return (userId, name);
        }


        //(Claim NameIdentifier, Claim Name) GetCllaimfromTokenWithoutLibrary(string accessToken)
        //{
        //    string[] parts = accessToken.Split('.');

        //    if (parts.Length != 3)
        //    {
        //        Console.WriteLine("Niepoprawny token");
        //        return;
        //    }

        //    string payload = parts[1];

        //    // JWT używa Base64Url, więc trzeba zamienić na Base64
        //    string base64 = payload.Replace('-', '+').Replace('_', '/');
        //    switch (base64.Length % 4)
        //    {
        //        case 2: base64 += "=="; break;
        //        case 3: base64 += "="; break;
        //        case 0: break;
        //        default:
        //            Console.WriteLine("Błąd paddingu base64");
        //            return;
        //    }

        //    byte[] bytes = Convert.FromBase64String(base64);
        //    string json = Encoding.UTF8.GetString(bytes);

        //    Console.WriteLine("Payload JSON:");
        //    Console.WriteLine(json);

        //    // Parsowanie JSON do dynamicznego obiektu:
        //    var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        //    Console.WriteLine($"name: {claims["name"]}");
        //    Console.WriteLine($"sub: {claims["sub"]}");

        //}

    }
}
