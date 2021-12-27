#nullable enable

using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace BMBF
{
    [Service(Label = "BMBFService", Name = "com.weareneutralaboutoculus.BMBFService")]
    // ReSharper disable once InconsistentNaming
    public class BMBFService : Service
    {
        // TODO: Is there a better way to do this than a static field?
        public static bool Running { get; private set; }

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
            Running = true;
        }

        private async Task StartWebServer()
        {
            try
            {
                _webHost = CreateHostBuilder();
                await _webHost.StartAsync();
                
                Log.Information("BMBF service startup complete");
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

        private void StopWebServer()
        {
            _webHost?.StopAsync().Wait();
            _webHost?.Dispose();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            Log.Information("Shutting down BMBFService");
            StopWebServer();
            Log.Information("Goodbye!");
            Log.CloseAndFlush();
        }

        private void SetupLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // Disable microsoft request logging
                .Enrich.FromLogContext()
                .WriteTo.AndroidLog(LogEventLevel.Verbose, "{Message:l}{NewLine}{Exception:l}")
                .WriteTo.File(Constants.LogPath, LogEventLevel.Verbose,
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger().ForContext<BMBFService>(); // Set default context to BMBF service
        }

        private IWebHost CreateHostBuilder()
        {
            var fileProvider = new AssetFileProvider(Assets ?? throw new NullReferenceException("Asset manager was null"));
            
            return WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((ctx, configBuilder) =>
                {
                    configBuilder.AddJsonFile(fileProvider, "appsettings.json", false, false);
                })
                .ConfigureLogging((_, logging) =>
                {
                    logging.ClearProviders();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(Assets);
                    services.AddSingleton<Service>(this);
                })
                .UseUrls(Constants.BindAddress)
                .UseSerilog()
                .Build();
        }
    }
}