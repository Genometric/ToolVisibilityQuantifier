﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;

namespace ToolShedCrawler
{
    public class Program
    {
        static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            options.Parse(args, out bool helpIsDisplayed);
            if (helpIsDisplayed)
                return;

            var services = new ServiceCollection();
            ConfigureServices(services);

            try
            {
                var serviceProvider = services.BuildServiceProvider();
                var crawler = serviceProvider.GetService<Crawler>();
                crawler.Crawl(options.CategoriesFilename, options.ToolsFilename, options.PublicationsFilename);
                Log.Debug("Crawler finished crawling successfully.");
            }
            catch(Exception e)
            {
                Log.Fatal(e, "Program terminated unexpectedly!");
            }
            finally
            {   
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog();
            });

            // Initialize serilog logger
            Log.Logger = new LoggerConfiguration()
                 .MinimumLevel.Debug()
                 .Enrich.FromLogContext()
                 .WriteTo.File("./log_file.log", LogEventLevel.Verbose)
                 .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                 .CreateLogger();

            // Add access to generic IConfigurationRoot
            services.AddSingleton(configuration);
            services.AddTransient<Crawler>();
        }
    }
}
