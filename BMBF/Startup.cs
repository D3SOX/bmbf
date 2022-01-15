﻿using BMBF.Configuration;
using BMBF.Extensions;
using BMBF.Implementations;
using BMBF.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BMBF;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Make sure to add controllers from the BMBF backend assembly
        var backendAssembly = typeof(ServiceCollectionExtensions).Assembly;
        services.AddMvc().AddApplicationPart(backendAssembly).AddControllersAsServices();
        
        services.AddBMBF();
        services.AddSingleton<IBeatSaberService, BeatSaberService>();
        services.AddSingleton<IAssetService, AssetService>();
        services.AddSingleton(_configuration.GetSection(BMBFResources.Position).Get<BMBFResources>());
        services.AddSingleton(_configuration.GetSection(BMBFSettings.Position).Get<BMBFSettings>());
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