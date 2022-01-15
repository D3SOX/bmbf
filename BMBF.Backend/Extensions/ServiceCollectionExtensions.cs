﻿using BMBF.Backend.Implementations;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using BMBF.Patching;
using Microsoft.Extensions.DependencyInjection;

namespace BMBF.Backend.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the BMBF backend services to the given service collection.
    /// It's important to note that <see cref="IBeatSaberService"/> and <see cref="IAssetProvider"/> must be registered
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