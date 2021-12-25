#nullable enable

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
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

            Running = true;
            _webHost = CreateHostBuilder();
            _webHost.StartAsync().ContinueWith(t => {
                if (t.IsCompletedSuccessfully)
                {
                    Log.Information("BMBF service startup complete");
                }
                else
                {
                    Running = false;
                    Log.Error("BMBF service startup failed");
                    if (t.Exception != null)
                    {
                        foreach (Exception ex in t.Exception.InnerExceptions)
                        {
                            Log.Error($"{ex}");
                        }
                    }
                    else
                    {
                        Log.Error("No exception found");
                    }
                }
            });
        }

        private void StopWebServer()
        {
            _webHost?.StopAsync().Wait();
            _webHost?.Dispose();
            Log.CloseAndFlush();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            Log.Information("Shutting down BMBFService");
            StopWebServer();
        }

        private void SetupLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(Constants.LogPath, LogEventLevel.Verbose, "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
        
        private IWebHost CreateHostBuilder()
        {
            return WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                })
                .UseUrls("http://0.0.0.0:50005")
                .UseSerilog()
                .Build();
        }
    }
}