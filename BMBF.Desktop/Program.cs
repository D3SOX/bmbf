using System;
using BMBF.Desktop;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Verbose() // Enable debug logs
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Avoid spammy request logging
    .CreateLogger(); 

using var host = WebHost.CreateDefaultBuilder()
    .ConfigureLogging((_, logging) =>
    {
        logging.ClearProviders();
    })
    .UseStartup<Startup>()
    .UseUrls("http://localhost:50006")
    .UseSerilog()
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