using AutoMapper;
using EFDataBase;
using FirebaseChachedDatabase;
using FirebaseDatabase;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ServiceMediatR.ListCommandAndQueries;
using Shared;
using ShoppingListWebApi.Auth.Api;
using ShoppingListWebApi.Data;
using ShoppingListWebApi.Handlers;
using ShoppingListWebApi.Hub;
using ShoppingListWebApi.Hub.Auth;
using ShoppingListWebApi.Token;
using SignalRService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ShoppingListWebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        public IConfiguration Configuration { get; }


        protected virtual void ConfigureDB(IServiceCollection services)
        {
           // services.AddDbContext<ShopingListDBContext>(options =>
           //        options.UseSqlite(Configuration.GetConnectionString("ShopingListDB3"), b => b.MigrationsAssembly("ShoppingListWebApi")));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();

            services.AddControllers().AddNewtonsoftJson(options =>
                  options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                ).AddApplicationPart(typeof(Startup).Assembly); ;

           

            ConfigureDB(services);

            string filepath = @"testnosqldb1-firebase-adminsdk-c123k-89b708d87e.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);


            // services.AddStackExchangeRedisCache(config =>
            // {
            //     //config.Configuration = _env.IsDevelopment()
            //     //    ? "127.0.0.1:6379"
            //     //    : Environment.GetEnvironmentVariable("REDIS_URL");

            //     config.Configuration = "127.0.0.1:6379";
            // }
            //);


            //services.AddDistributedMemoryCache();

            //-----------------------------------------------
            //services.AddDbContext<ShopingListDBContext>(options =>
            //       //options.UseSqlServer(Configuration.GetConnectionString("ShopingListDB2")));
            //       options.UseSqlite(Configuration.GetConnectionString("ShopingListDB3")));

            //services.AddEFDatabase();

            //---------------------------------------------
            //services.AddFirebasedDatabase();

            //------------------------------------------------
            services.AddFirebaseCaschedDatabas();
            services.AddSingleton<CacheConveinient>();
            services.AddSingleton<IMiniDistributedCache, FirabaseCache>();
            //-----------------------------------------------

            services.AddSingleton<ITokenService, TokenService>();


            services.AddAutoMapper(typeof(MappingProfile));

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });


            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
          .AddJwtBearer(x =>
          {
              x.RequireHttpsMetadata = true;
              x.SaveToken = true;
              x.TokenValidationParameters = new TokenValidationParameters
              {
                  ValidateIssuerSigningKey = true,
                  IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetSection("Secrets")["JWTSecurityKey"])),
                  //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("eashfisahfihgiuashrilghas9ifhiuhvi9uashblvh938hen48239")),
                  ValidateIssuer = false,
                  ValidateAudience = false,
                  ValidateLifetime = true,
                  //ClockSkew = TimeSpan.FromDays(1)
                  ClockSkew = TimeSpan.FromMilliseconds(100)

              };
              x.Events = new JwtBearerEvents
              {
                  OnAuthenticationFailed = context =>
                  {
                      if (context.Exception is SecurityTokenExpiredException)
                      {
                          context.Response.Headers.Add("Token-Expired", "true");
                      }
                      return Task.CompletedTask;
                  }
              };
          }).AddJwtBearer("NoLifetimeBearer", options =>
          {
              options.TokenValidationParameters = new TokenValidationParameters
              {
                  ValidateIssuer = false,
                  ValidateAudience = false,
                  ValidateLifetime = false,
                  ValidateIssuerSigningKey = true,
                  IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetSection("Secrets")["JWTSecurityKey"])),
              }; 
          });
            services.AddHttpContextAccessor();
            services.AddScoped<IAuthorizationHandler, CustomRequirePermissionLevelHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, CustomAuthorizationPolicyProvider>();
            services.AddSingleton<SignarRService>();
            services.AddMediatR(typeof(AddListCommand).Assembly);

            services.AddCors(options =>
            {
                options.AddPolicy("ALL", builder =>
                {
                    builder.WithOrigins("https://localhost:44379", "https://localhost:5003"
                        , "https://localhost:5023"
                        , "https://shoppinglist2.mcfly.ga", "https://shoppinglist.mcfly.ga"
                        , "http://localhost:52735");
                    //builder.AllowAnyOrigin();
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    //builder.AllowCredentials();

                });
            });
            //------------------------------------------------
            services.AddAuthentication()
               .AddScheme<AuthenticationSchemeOptions, CustomSchemeHandler>(CustomSchemeHandler.CustomScheme, _ =>
               {
               });
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
            services.AddScoped<AuthService>();
            services.AddSignalR();
            //------------------------------------------------

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var serviceProvider = app.ApplicationServices.CreateScope().ServiceProvider;
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            //var dbContext = serviceProvider.GetRequiredService<ShopingListDBContext>();

            //loggerFactory.AddFile("R:\\111.txt");
            //loggerFactory.AddProvider(new Logging.AppLoggerProvider(app.ApplicationServices, httpContextAccessor));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "text/html";

                        await context.Response.WriteAsync("<html lang=\"en\"><body>\r\n");
                        await context.Response.WriteAsync("ERROR!<br><br>\r\n");

                        var exceptionHandlerPathFeature =
                            context.Features.Get<IExceptionHandlerPathFeature>();

                        // Use exceptionHandlerPathFeature to process the exception (for example, 
                        // logging), but do NOT expose sensitive error information directly to 
                        // the client.

                        if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
                        {
                            await context.Response.WriteAsync("File error thrown!<br><br>\r\n");
                        }

                        await context.Response.WriteAsync(exceptionHandlerPathFeature?.Error.Message + "<br><br>\r\n");
                        await context.Response.WriteAsync(exceptionHandlerPathFeature?.Error.InnerException?.Message + "< br><br>\r\n");


                        await context.Response.WriteAsync("<a href=\"/\">Home</a><br>\r\n");
                        await context.Response.WriteAsync("</body></html>\r\n");
                        await context.Response.WriteAsync(new string(' ', 512)); // IE padding
                    });
                });
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            // app.UseMiddleware<MiddlewareSignalR>();
            //app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors("ALL");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SendRequest>("/chatHub");
            });

        }
    }
}
