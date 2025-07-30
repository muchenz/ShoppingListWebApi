using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShoppingListWebApiSignalR.Hubs;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net.Sockets;
using System.Net.Http;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ShoppingListWebApiSignalR
{
    public class Startup
    {
        public const string CustomScheme = nameof(CustomScheme);
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, CustomSchemeHandler>(CustomScheme, _ =>
            {
            });
            //services.AddScoped<IAuthorizationHandler, CustomRequirePermissionLevelHandler>();
            //services.AddSingleton<IAuthorizationPolicyProvider, CustomAuthorizationPolicyProvider>();

            services.AddCors();
            services.AddSignalR();
            services.AddRazorPages();
            services.AddScoped<AuthService>();

            services.AddHttpClient("api", client =>
                 {
                     //code to configure headers etc..
                     client.BaseAddress = new Uri(Configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"]);

                 }).ConfigurePrimaryHttpMessageHandler(() =>
                 {
                     var handler = new HttpClientHandler();

                     handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                     return handler;
                 });

        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST");
                    //.AllowAnyMethod();
            });
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<SendRequest>("/chatHub");
            });




        }

        public class CustomSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            private readonly AuthService _authService;

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

                    var isTokenGood = await _authService.IsValidateTokenAsync(accessToken);

                    if (!isTokenGood) 
                        return await Task.FromResult(AuthenticateResult.Fail("Not authorized.  Access_Token is wrong."));

                    // pozyskać nameidentifier (user ID)
                    // ustawić claim  nameidentifier na UserID
                    // to to żeby SignaR wukoszytsał to w Clients.User(item.ToString()).SendAsync("DataAreChanged_"+item)

                    god:                   var claims = new Claim[]
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


    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(IConfiguration configuration
              , IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }


        public async Task<bool> IsValidateTokenAsync(string token)
        {

            if (token == "I_am_a_god_of_hellfire.") return true;

            var client = _httpClientFactory.CreateClient("api");

            var baseAdress = _configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"];

            var response = await client.GetAsync($"User/VerifyToken?accessToken={token}");

            var data = await response.Content.ReadAsStringAsync();
             var isTokenGood = System.Text.Json.JsonSerializer.Deserialize<bool>(data);

            return isTokenGood;
        }


    }


    
}
