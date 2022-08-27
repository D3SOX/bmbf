using System;
using System.IO;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Services;
using BMBF.Desktop;
using BMBF.Desktop.Configuration;
using BMBF.Desktop.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Verbose() // Enable debug logs
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Avoid spammy request logging
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .CreateLogger();

var assetFileProvider = new PhysicalFileProvider(Path.GetFullPath(Constants.AssetsPath));
var webRootFileProvider = new PhysicalFileProvider(Path.GetFullPath(Constants.WebRootPath));

using var host = Host.CreateDefaultBuilder()
    .ConfigureLogging(logBuilder => logBuilder.ClearProviders())
    .ConfigureServices((ctx, services) =>
    {
        var configuration = ctx.Configuration;
        var settings = configuration.GetSection(BMBFSettings.Position).Get<BMBFSettings>();
        var resources = configuration.GetSection(BMBFResources.Position).Get<BMBFResources>();
        var desktopSettings = configuration.GetSection(BMBFDesktopSettings.Position).Get<BMBFDesktopSettings>();

        // Update paths in the settings to make sure they're all within our device directory
        services.AddSingleton(desktopSettings);
        services.AddSingleton<IBeatSaberService, BeatSaberService>();

        services.AddBMBF(settings, resources, assetFileProvider, webRootFileProvider);
    })
    .ConfigureAppConfiguration(configBuilder =>
    {
        // Remove the existing appsettings.json
        configBuilder.Sources.Clear();

        // Make sure to add the BMBF.Desktop appsettings AFTER those from regular BMBF
        // This is to allow us to override file paths
        configBuilder.AddJsonFile(assetFileProvider, "appsettings.json", false, false);
        configBuilder.AddJsonFile("appsettings.json");
    })
    .UseSerilog()
#if DEBUG
    .UseEnvironment(Environments.Development)
#endif
    .Build();

Log.Information("Starting up BMBF");
await host.StartAsync();
Log.Information("BMBF startup complete");

Console.ReadKey();
await host.StopAsync();
host.Dispose();
Log.Information("Goodbye!");
Log.CloseAndFlush();
