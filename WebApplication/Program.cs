using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using System;
using System.IO;
using System.Reflection;

namespace WebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            IConfiguration config = new ConfigurationBuilder()
                       .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
                       .AddJsonFile("appsettings.json", false, false)
                       .AddJsonFile($"appsettings.{environment}.json", true, false)
                       .AddEnvironmentVariables()
                       .Build();

            LogManager.Setup().LoadConfigurationFromSection(config, "NLog");

            var logger = LogManager.GetCurrentClassLogger();

            try
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                logger.Info($"Start version: {version}, with environment: {environment}");
                CreateHostBuilder(args, config).Build().Run();
            }
            catch (Exception ex)
            {
                //NLog: catch setup errors
                logger.Error(ex, "Stopped program because of exception");
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration externalConfig) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.Sources.Clear();
                        config.AddConfiguration(externalConfig);
                    });
                    webBuilder.UseConfiguration(externalConfig);
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        // Override the default logging level (Information)
                        loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    });
                    webBuilder.UseNLog();
                });
    }
}
