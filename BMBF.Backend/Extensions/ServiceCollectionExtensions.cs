using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Reflection;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using BMBF.Patching;
using BMBF.QMod;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BMBF.Backend.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the BMBF backend services to the given service collection.
    /// It's important to note that <see cref="IBeatSaberService"/> must be registered separately
    /// separately - no implementation is provided in this project.
    /// </summary>
    /// <param name="services">Service collection to add the BMBF backend services to</param>
    /// <param name="ctx">Context of the web app</param>
    /// <param name="settings">BMBF settings</param>
    /// <param name="resources">BMBF resource URLs</param>
    /// <param name="assetFileProvider">File provider used to load assets such as built-in core mods,
    /// libunity.so, and modloader. If null, then built in assets will not be used, and these files
    /// will always be downloaded manually</param>
    public static void AddBMBF(this IServiceCollection services,
                                WebHostBuilderContext ctx, 
                                BMBFSettings settings,
                                BMBFResources resources,
                                IFileProvider assetFileProvider)
    {
        if (ctx.HostingEnvironment.IsDevelopment())
        {
            services.AddSwaggerGen(options =>
            {
                options.SupportNonNullableReferenceTypes();
            });
        }
        services.AddRouting(options => options.LowercaseUrls = true);
        
        services.AddSingleton<ISongService, SongService>();
        services.AddSingleton<IPlaylistService, PlaylistService>();
        services.AddSingleton<ISetupService, SetupService>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IFileImporter, FileImporter>();
        services.AddSingleton<IBeatSaverService, BeatSaverService>();
        services.AddSingleton<IAssetService, AssetService>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton(settings);
        services.AddSingleton(resources);
        services.AddSingleton(assetFileProvider);
        services.AddSingleton(HttpClientUtil.CreateBMBFHttpClient());

        services.AddSingleton<ModService>();
        services.AddSingleton<IModService>(s =>
        {
            var modService = s.GetService<ModService>() ?? throw new NullReferenceException($"{nameof(ModService)} not configured");
            
            // Add a QModProvider - more mod providers can be registered here to support alternative mod types
            var provider = new QModProvider(
                settings.PackageId,
                settings.ModFilesPath,
                settings.LibFilesPath,
                s.GetService<HttpClient>() ?? throw new NullReferenceException("No HttpClient configured"),
                new FileSystem(),
                modService
            );

            modService.RegisterProvider(provider);
            return modService;
        });
        
        // Add controllers, making sure to include those in this assembly instead of only those in the entry assembly 
        var mvcBuilder = services.AddControllers();
        mvcBuilder.PartManager
            .ApplicationParts
            .Add(new AssemblyPart(Assembly.GetExecutingAssembly()));
        
        // Configure our legacy tags
        var tagManager = new TagManager();
        tagManager.RegisterLegacyTag("modded",
            () => new PatchManifest("QuestPatcher", null) { ModloaderName = "QuestLoader" });
        tagManager.RegisterLegacyTag("BMBF.modded",
            () => new PatchManifest("BMBF", null) { ModloaderName = "QuestLoader" });

        services.AddSingleton(tagManager);
    }
}