using System.IO.Abstractions;
using System.Net.Http;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using BMBF.Patching;
using BMBF.QMod;
using Microsoft.Extensions.DependencyInjection;

namespace BMBF.Backend.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the BMBF backend services to the given service collection.
    /// It's important to note that <see cref="IBeatSaberService"/> must be registered separately
    /// separately - no implementation is provided in this project.
    /// </summary>
    /// <param name="services">Service collection to add the BMBF backend services to</param>
    // ReSharper disable once InconsistentNaming
    public static void AddBMBF(this IServiceCollection services)
    {
        services.AddSingleton<ISongService, SongService>();
        services.AddSingleton<IPlaylistService, PlaylistService>();
        services.AddSingleton<ISetupService, SetupService>();
        services.AddSingleton<IMessageService, MessageService>();
        
        services.AddSingleton<ModService>();
        services.AddSingleton<IModService>(s =>
        {
            var settings = s.GetService<BMBFSettings>();
            var modService = s.GetService<ModService>();
            
            var provider = new QModProvider(
                settings.PackageId,
                settings.ModFilesPath,
                settings.LibFilesPath,
                s.GetService<HttpClient>(),
                new FileSystem(),
                modService
            );

            modService.RegisterProvider(provider);
            return modService;
        });
            
        services.AddSingleton(HttpClientUtil.CreateBMBFHttpClient());
        services.AddSingleton<IExtensionsService, ExtensionsService>();
        services.AddSingleton<IFileImporter, FileImporter>();
        services.AddSingleton<IBeatSaverService, BeatSaverService>();
        services.AddSingleton<IAssetService, AssetService>();

        // Configure our legacy tags
        var tagManager = new TagManager();
        tagManager.RegisterLegacyTag("modded",
            () => new PatchManifest("QuestPatcher", null) { ModloaderName = "QuestLoader" });
        tagManager.RegisterLegacyTag("BMBF.modded",
            () => new PatchManifest("BMBF", null) { ModloaderName = "QuestLoader" });

        services.AddSingleton(tagManager);
    }
}