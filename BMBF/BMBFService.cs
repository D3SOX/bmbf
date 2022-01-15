using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Java.Lang;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Exception = System.Exception;

namespace BMBF;

[Service(Name = "com.weareneutralaboutoculus.BMBFService", Label = "BMBFService")]
// ReSharper disable once InconsistentNaming
public class BMBFService : Service
{
    public static string? RunningUrl { get; private set; }
        
    private IWebHost? _webHost;

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override void OnCreate()
    {
        base.OnCreate();

        SetupLogging();
        Log.Information("BMBF service starting up!");
            
        Task.Run(async () => await StartWebServer());
    }

    private async Task StartWebServer()
    {
        try
        {
            _webHost = CreateHostBuilder();
            await _webHost.StartAsync();
                
            Log.Information("BMBF service startup complete");
            RunningUrl = Constants.BindAddress; // Notify future activity startups that the service has already started
            Intent intent = new Intent(BMBFIntents.WebServerStartedIntent);
            intent.PutExtra("BindAddress", Constants.BindAddress);
            SendBroadcast(intent);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BMBF webserver startup failed");
            Intent intent = new Intent(BMBFIntents.WebServerFailedToStartIntent);
            intent.PutExtra("Exception", ex.ToString());
            SendBroadcast(intent);
        }
    }

    private async Task StopWebServerAsync()
    {
        if (_webHost == null) return;

        try
        {
            await _webHost.StopAsync().ConfigureAwait(false);
            _webHost.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to shutdown web host");
        }
        RunningUrl = null;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
            
        Log.Information("Shutting down web host");
        StopWebServerAsync().Wait();
        Log.Information("Goodbye!");
        Log.CloseAndFlush(); 
    }

    private void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Disable microsoft request logging
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.AndroidLog(LogEventLevel.Verbose, "{Message:l}{NewLine}{Exception:l}")
            .WriteTo.File(Constants.LogPath, LogEventLevel.Verbose,
                "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger().ForContext<BMBFService>(); // Set default context to BMBF service
    }

    private IWebHost CreateHostBuilder()
    {
        var assetFileProvider = new AssetFileProvider(Assets ?? throw new NullReferenceException("Asset manager was null"));
            
        return WebHost.CreateDefaultBuilder()
            .ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.Sources.Clear();
                configBuilder.AddJsonFile(assetFileProvider, "appsettings.json", false, false);
            })
            .ConfigureLogging((_, logging) =>
            {
                logging.ClearProviders();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<Service>(this);
                services.AddSingleton<IFileProvider>(assetFileProvider);
            })
            .UseStartup<Startup>()
            .UseUrls(Constants.BindAddress)
            .UseSerilog()
            .Build();
    }
        
    private void SetupForegroundService()
    {
        var serviceChannel = new NotificationChannel(
            "BMBF",
            "BMBF",
            NotificationImportance.Default
        );

        var manager = (NotificationManager) GetSystemService(Class.FromType(typeof(NotificationManager)))!;
        manager.CreateNotificationChannel(serviceChannel);

        var notificationIntent = new Intent(this, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, 0)!;

        // Add a notification to prevent our service from getting killed
        var notification = new Notification.Builder(this, "BMBF")
            .SetContentTitle("BMBF Background Service")
            .SetContentText("BMBF is running in the background")
            .SetContentIntent(pendingIntent)
            .Build();
        StartForeground(1, notification);
    }
        
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags startCommandFlags, int startId) {
        // If BMBF is configured to run in the background permanently, we are starting as a foreground service
        // Foreground services run forever once they have a notification registered, so we register the notification now
        if (System.IO.File.Exists(Constants.RunForegroundConfig))
        {
            SetupForegroundService();
            return StartCommandResult.NotSticky;
        }
        return StartCommandResult.Sticky;
    }
}