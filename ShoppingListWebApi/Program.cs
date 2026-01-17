using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShoppingListWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                //.ConfigureAppConfiguration((hostingContext, config) =>
                //{
                //    var env = hostingContext.HostingEnvironment;

                //    // find the shared folder in the parent folder
                //    var sharedFolder = Path.Combine(env.ContentRootPath, "/api");

                //    //load the SharedSettings first, so that appsettings.json overrwrites it
                //    config
                //        .AddJsonFile(Path.Combine(sharedFolder, "SharedSettings.json"), optional: true)
                //        .AddJsonFile("appsettings.json", optional: true)
                //        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

                //    config.AddEnvironmentVariables();
                //})
                .Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                     //webBuilder.UseUrls("http://0.0.0.0:5003");
                     //webBuilder.UseUrls("https://127.0.0.1:5003");
                });
    }
}
