using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using EFDataBase;
using FirebaseDatabase;
using MediatR;
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
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ServiceMediatR.ListCommandAndQueries;
using Shared;
using ShoppingListWebApi.Data;
using ShoppingListWebApi.Handlers;
using SignalRService;

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
            services.AddDbContext<ShopingListDBContext>(options =>
                   options.UseSqlite(Configuration.GetConnectionString("ShopingListDB3"), b => b.MigrationsAssembly("ShoppingListWebApi")));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson(options =>
                  options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                ).AddApplicationPart(typeof(Startup).Assembly); ;

            //services.AddDbContext<ShopingListDBContext>(options =>
            //        options.UseSqlServer(Configuration.GetConnectionString("ShopingListDB2")));

            ConfigureDB(services);

            string filepath = @"e:\testnosqldb1-firebase-adminsdk-c123k-89b708d87e.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);


            //services.AddTransient<IUserEndpoint, UserEndpoint>();
            //services.AddTransient<IListAggregatorEndpoint, ListAggregatorEndpoint>();
            //services.AddTransient<IListItemEndpoint, ListItemEndpoint>();
            //services.AddTransient<IInvitationEndpoint, InvitationEndpoint>();
            //services.AddTransient<IListEndpoint, ListEndpoint>();

            services.AddTransient<IUserEndpoint, UserEndpointFD>();
            services.AddTransient<IListAggregatorEndpoint, ListAggregatorEndpointFD>();
            services.AddTransient<IListItemEndpoint, ListItemEndpointFD>();
            services.AddTransient<IInvitationEndpoint, InvitationEndpointFD>();
            services.AddTransient<IListEndpoint, ListEndpointFD>();

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
                  IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("Secrets")["JWTSecurityKey"])),
                  //IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("iausdfhseui68t776t8t8gt6gb8igt8ogtolgt8o7gt8o7gt8gt80weidh0wehidn")),
                  ValidateIssuer = false,
                  ValidateAudience = false,
                  ValidateLifetime = true,
                  ClockSkew = TimeSpan.FromDays(1)
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
                        , "https://shoppinglist2.mcfly.ml/", "https://shoppinglist.mcfly.ml/");
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    //builder.AllowCredentials();

                });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var serviceProvider = app.ApplicationServices.CreateScope().ServiceProvider;
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            //var dbContext = serviceProvider.GetRequiredService<ShopingListDBContext>();

            loggerFactory.AddFile("R:\\111.txt");
            loggerFactory.AddProvider(new Logging.AppLoggerProvider(app.ApplicationServices, httpContextAccessor));

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
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors("ALL");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
