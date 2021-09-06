using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BlazorClient.Data;
using BlazorClient.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IO;
using Blazored.Modal;
using System.Net.Http;
using Blazored.Toast;
using Microsoft.Extensions.Logging;
using ShoppingListWebApi.Logging;
using BlazorClient.Pages.LoginPages;
using Microsoft.AspNetCore.Http;
using BlazorClient.Handlers;

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
            services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; }); ;
            services.AddSingleton<WeatherForecastService>();
            services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            services.AddScoped<CustomAuthorizationHeaderHandler>();


            // services.AddHttpClient<UserService>();
            // services.AddHttpClient<ShoppingListService>();

            services.AddHttpClient<UserService>(client => {
                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            });//.AddHttpMessageHandler<CustomAuthorizationHeaderHandler>(); ;

            services.AddHttpClient<ShoppingListService>(client => {
                // code to configure headers etc..
            }).ConfigurePrimaryHttpMessageHandler(() => {
                var handler = new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                return handler;
            });

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
