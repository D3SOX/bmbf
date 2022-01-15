using System.IO;
using BMBF.Backend;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Services;
using BMBF.Desktop.Configuration;
using BMBF.Desktop.Implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace BMBF.Desktop;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string CombineDisableAbsolute(string path1, string path2)
    {
        if (Path.IsPathRooted(path2)) path2 = path2.Substring(1);
        return Path.Combine(path1, path2);
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        var backendAssembly = typeof(ServiceCollectionExtensions).Assembly;
        
        services.AddMvc().AddApplicationPart(backendAssembly).AddControllersAsServices();
        services.AddBMBF();
        
        var settings = _configuration.GetSection(BMBFSettings.Position).Get<BMBFSettings>();
        var desktopSettings = _configuration.GetSection(BMBFDesktopSettings.Position).Get<BMBFDesktopSettings>();
        var deviceRoot = desktopSettings.DeviceRoot;
        
        // Update paths in the settings to make sure they're all within our device directory
        settings.ConfigsPath = CombineDisableAbsolute(deviceRoot, settings.ConfigsPath);        
        settings.SongsPath = CombineDisableAbsolute(deviceRoot, settings.SongsPath);        
        settings.PlaylistsPath = CombineDisableAbsolute(deviceRoot, settings.PlaylistsPath);        
        settings.RootDataPath = CombineDisableAbsolute(deviceRoot, settings.RootDataPath);

        services.AddSingleton(settings);
        services.AddSingleton(desktopSettings);
        services.AddSingleton(_configuration.GetSection(BMBFResources.Position).Get<BMBFResources>());

        services.AddSingleton<IBeatSaberService, BeatSaberService>();
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseWebSockets();
        app.UseMiddleware<Middleware>();
        app.UseMvc();
    }
}