using System;
using System.IO;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Services;
using BMBF.Desktop;
using BMBF.Desktop.Configuration;
using BMBF.Desktop.Implementations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;


string CombineDisableAbsolute(string path1, string path2)
{
    if (Path.IsPathRooted(path2)) path2 = path2.Substring(1);
    return Path.Combine(path1, path2);
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Verbose() // Enable debug logs
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Avoid spammy request logging
    .CreateLogger();

var assetFileProvider = new PhysicalFileProvider(Path.GetFullPath(Constants.AssetsPath));
var webRootFileProvider = new PhysicalFileProvider(Path.GetFullPath(Constants.WebRootPath));

using var host = WebHost.CreateDefaultBuilder()
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders(); // Remove default asp.net core logging
    })
    .ConfigureServices((ctx, services) =>
    {
        var configuration = ctx.Configuration;
        var settings = configuration.GetSection(BMBFSettings.Position).Get<BMBFSettings>();
        var resources = configuration.GetSection(BMBFResources.Position).Get<BMBFResources>();
        var desktopSettings = configuration.GetSection(BMBFDesktopSettings.Position).Get<BMBFDesktopSettings>();
        var deviceRoot = desktopSettings.DeviceRoot;
        
        // Update paths in the settings to make sure they're all within our device directory
        settings.ConfigsPath = CombineDisableAbsolute(deviceRoot, settings.ConfigsPath);        
        settings.SongsPath = CombineDisableAbsolute(deviceRoot, settings.SongsPath);        
        settings.PlaylistsPath = CombineDisableAbsolute(deviceRoot, settings.PlaylistsPath);        
        settings.RootDataPath = CombineDisableAbsolute(deviceRoot, settings.RootDataPath);
        settings.ModFilesPath = CombineDisableAbsolute(deviceRoot, settings.ModFilesPath);
        settings.LibFilesPath = CombineDisableAbsolute(deviceRoot, settings.LibFilesPath);
        services.AddSingleton(desktopSettings);
        services.AddSingleton<IBeatSaberService, BeatSaberService>();
        
        services.AddBMBF(ctx, settings, resources, assetFileProvider);
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
    .Configure((ctx, app) => app.UseBMBF(ctx, webRootFileProvider))
    .UseUrls("http://localhost:50006")
    .UseSerilog()
#if DEBUG
    .UseEnvironment(Environments.Development)
#endif
    .Build();

var shutdownTriggered = false;

Log.Information("Starting web host");
await host.StartAsync();
Log.Information("BMBF startup complete");

Console.CancelKeyPress += async (_, _) =>
{
    if (shutdownTriggered) return;
    
    Log.Information("Shutting down");
    shutdownTriggered = true;
    try
    {
        await host.StopAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex,"Failed to shut down");
    }
};

await host.WaitForShutdownAsync();
Log.Information("Goodbye!");
Log.CloseAndFlush();