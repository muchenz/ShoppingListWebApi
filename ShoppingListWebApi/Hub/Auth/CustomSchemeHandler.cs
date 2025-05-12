using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System;

namespace ShoppingListWebApi.Hub.Auth
{
    public class CustomSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AuthService _authService;

        public const string CustomScheme = nameof(CustomScheme);

        public CustomSchemeHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
            , AuthService authService

        ) : base(options, logger, encoder, clock)
        {
            _authService = authService;
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

                var isTokenGood = await _authService.IsValidateToken2Async(accessToken);
                //var isTokenGood = await _authService.IsValidateTokenAsync(accessToken);

                if (!isTokenGood)
                    return await Task.FromResult(AuthenticateResult.Fail("Not authorized.  Access_Token is wrong."));


                // pozyskać nameidentifier (user ID)
                // ustawić claim  nameidentifier na UserID
                // to to żeby SignaR wukoszytsał to w Clients.User(item.ToString()).SendAsync("DataAreChanged_"+item)

                god: var claims = new Claim[]
                     {
                         // new("user_id", cookie),
                         //new("cookie", "cookie_claim"),
                     };
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
    }
}
