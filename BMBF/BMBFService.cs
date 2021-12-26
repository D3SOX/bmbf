#nullable enable

using System;
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
                .WriteTo.AndroidLog(LogEventLevel.Verbose, "BMBF [{Level}] {Message:l{NewLine:l}{Exception:l}")
                .WriteTo.File(Constants.LogPath, LogEventLevel.Verbose, "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
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
                .ConfigureServices(services => services.AddSingleton(Assets))
                .UseUrls(Constants.BindAddress)
                .UseSerilog()
                .Build();
        }
    }
}