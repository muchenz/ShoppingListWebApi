using BlazorClient.Data;
using BlazorClient.Handlers;
using BlazorClient.Pages.LoginPages;
using BlazorClient.Services;
using Blazored.LocalStorage;
using Blazored.Modal;
using Blazored.Toast;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShoppingListWebApi.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorClient
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
            services.AddSingleton<WeatherForecastService>();
            services.AddScoped<StateService>();
            services.AddScoped<TokenClientService>();
            services.AddScoped<TokenHttpClient>();
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            services.AddScoped<CustomAuthorizationHeaderHandler>();
            services.AddScoped<SignalRService>();
            services.AddScoped<AuthRedirectHandler>();

            // services.AddHttpClient<UserService>();
            // services.AddHttpClient<ShoppingListService>();
            services.AddHttpClient<TokenClientService>(client => {
                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            }).AddHttpMessageHandler<AuthRedirectHandler>(); ;//.AddHttpMessageHandler<CustomAuthorizationHeaderHandler>(); ;
            services.AddHttpClient<UserService>(client => {
                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            }).AddHttpMessageHandler<AuthRedirectHandler>(); ;//.AddHttpMessageHandler<CustomAuthorizationHeaderHandler>(); ;

            services.AddHttpClient<ShoppingListService>(client => {
                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            }).AddHttpMessageHandler<AuthRedirectHandler>();

            services.AddHttpClient("api", client => {
                client.BaseAddress = new Uri(Configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"]);

                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            }).AddHttpMessageHandler<AuthRedirectHandler>();

            services.AddHttpClient("log", client => {
                // code to configure headers etc..
                client.BaseAddress = new Uri(Configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"]);
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            });//.AddHttpMessageHandler<CustomAuthorizationHeaderHandler>(); 

           
            services.AddScoped<BrowserService>();

            services.AddBlazoredModal();
            services.AddBlazoredLocalStorage();
            services.AddBlazoredToast();

            services.AddLogging(logging => {
              
                logging.SetMinimumLevel(LogLevel.Error);
                //logging.ClearProviders();
            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {


            var serviceProvider = app.ApplicationServices.CreateScope().ServiceProvider;
            var httpFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpFactory.CreateClient("log");
            var authenticationStateProvider = serviceProvider.GetRequiredService<AuthenticationStateProvider>();
           
         //   loggerFactory.AddProvider(new AppLoggerProvider(httpClient, authenticationStateProvider));

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

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
public class AuthRedirectHandler : DelegatingHandler
{
    private readonly NavigationManager _navigation;

    public AuthRedirectHandler(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        var isRejectedByExpiredToken = false;
        if(response.Headers.TryGetValues("Token-Expired", out var values))
        {
            isRejectedByExpiredToken = bool.Parse(values.FirstOrDefault("false"));
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            !request.RequestUri.AbsolutePath.Contains("user/login", StringComparison.OrdinalIgnoreCase) &&
            !isRejectedByExpiredToken)
        {
           throw new UnauthorizedAccessException();

            //nie działa: 
            //_navigation.NavigateTo("/login", forceLoad: true);`
        }

        return response;
    }
}