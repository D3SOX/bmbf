using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using BMBF.Backend.Configuration;
using BMBF.Backend.Endpoints;
using BMBF.Backend.Implementations;
using BMBF.Backend.Services;
using BMBF.Patching;
using BMBF.QMod;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Serilog;
using SongFeedReaders.Feeds;
using SongFeedReaders.Feeds.BeastSaber;
using SongFeedReaders.Feeds.BeatSaver;
using SongFeedReaders.Feeds.ScoreSaber;
using WebUtilities;
using WebUtilities.HttpClientWrapper;

namespace BMBF.Backend.Extensions;

public static class ServiceCollectionExtensions
{
    private static string UserAgent => $"BMBF/{Assembly.GetExecutingAssembly().GetName().Version}";

    /// <summary>
    /// Configures the default BMBF headers on the given <see cref="HttpClient"/>
    /// </summary>
    /// <param name="httpClient">Client to configure</param>
    private static void ConfigureDefaults(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    /// <summary>
    /// Adds the BMBF backend services to the given service collection.
    /// It's important to note that <see cref="IBeatSaberService"/> must be registered separately
    /// separately - no implementation is provided in this project.
    /// </summary>
    /// <param name="services">Service collection to add the BMBF backend services to</param>
    /// <param name="settings">BMBF settings</param>
    /// <param name="resources">BMBF resource URLs</param>
    /// <param name="assetFileProvider">File provider used to load assets such as built-in core mods,
    /// libunity.so, and modloader. If no files exist, then built in assets will not be used, and these files
    /// will always be downloaded manually</param>
    /// <param name="webRootFileProvider">File provider for static web files</param>
    public static void AddBMBF(this IServiceCollection services,
        BMBFSettings settings,
        BMBFResources resources,
        IFileProvider assetFileProvider,
        IFileProvider webRootFileProvider)
    {
        // Add key BMBF services
        services.AddSingleton<ISongService, SongService>();
        services.AddSingleton<IPlaylistService, PlaylistService>();
        services.AddSingleton<ISetupService, SetupService>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IFileImporter, FileImporter>();
        services.AddSingleton<IAssetService, AssetService>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IBeatSaverService, BeatSaverService>();
        services.AddSingleton<ICoreModService, CoreModService>();
        services.AddSingleton<IProgressService, ProgressService>();
        services.AddSingleton<ISyncSaberService, SyncSaberService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddTransient<IFileSystemWatcher>(s =>
            s.GetRequiredService<IFileSystem>()
                .FileSystemWatcher.CreateNew());
        services.AddHostedService<WebService>();
        
        // Add API endpoints
        services.AddTransient<IEndpoints, InfoEndpoints>();
        services.AddTransient<IEndpoints, BeatSaberEndpoints>();
        services.AddTransient<IEndpoints, ModsEndpoints>();
        services.AddTransient<IEndpoints, PlaylistsEndpoints>();
        services.AddTransient<IEndpoints, SetupEndpoints>();
        services.AddTransient<IEndpoints, SongsEndpoints>();
        services.AddTransient<IEndpoints, ImportEndpoints>();
        services.AddTransient<IEndpoints, WebSocketEndpoints>();
        services.AddTransient<IEndpoints, SyncSaberEndpoints>();
        services.AddTransient<IEndpoints, ProgressEndpoints>();
        services.AddSingleton<AuthEndpoints>();

        // SongFeedReaders services
        services.AddTransient<IFeed, BeatSaverLatestFeed>();
        services.AddTransient<IFeed, BeastSaberBookmarksFeed>();
        services.AddTransient<IFeed, BeatSaverMapperFeed>();
        services.AddTransient<IFeed, BeastSaberCuratorFeed>();
        services.AddTransient<IFeed, ScoreSaberLatestFeed>();
        services.AddTransient<IFeed, ScoreSaberTrendingFeed>();
        services.AddTransient<IFeed, ScoreSaberTopRankedFeed>();
        services.AddTransient<IFeed, ScoreSaberTopPlayedFeed>();
        services.AddTransient<IBeastSaberPageHandler, BeastSaberPageHandler>();
        services.AddTransient<IBeatSaverPageHandler, BeatSaverPageHandler>();
        services.AddTransient<IScoreSaberPageHandler, ScoreSaberPageHandler>();
        var webClient = new HttpClientWrapper();
        webClient.SetUserAgent(UserAgent);
        services.AddSingleton<IWebClient>(webClient);

        services.AddSingleton<IFeedSettings>(new BeatSaverLatestSettings());
        services.AddSingleton<IFeedSettings>(new BeastSaberBookmarksSettings());
        services.AddSingleton<IFeedSettings>(new BeatSaverMapperSettings());
        services.AddSingleton<IFeedSettings>(new BeastSaberCuratorSettings());
        services.AddSingleton<IFeedSettings>(new ScoreSaberLatestSettings());
        services.AddSingleton<IFeedSettings>(new ScoreSaberTrendingSettings());
        services.AddSingleton<IFeedSettings>(new ScoreSaberTopRankedSettings());
        services.AddSingleton<IFeedSettings>(new ScoreSaberTopPlayedSettings());

        // Add the default JSON serializer options
        services.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        // Add HTTP clients for requests to external services
        services.AddHttpClient<IBeatSaverService, BeatSaverService>(client =>
        {
            ConfigureDefaults(client);
            client.BaseAddress = settings.BeatSaverBaseUri;
        });
        services.AddHttpClient<IAssetService, AssetService>(ConfigureDefaults);
        services.AddHttpClient();
        
        services.AddSingleton(settings);
        services.AddSingleton(resources);
        services.AddSingleton(new FileProviders(assetFileProvider, webRootFileProvider));

        services.AddSingleton<ModService>();
        services.AddSingleton<IModService>(s =>
        {
            var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
            var modService = s.GetRequiredService<ModService>();
            
            var httpClient = httpClientFactory.CreateClient();
            ConfigureDefaults(httpClient);

            // Add a QModProvider - more mod providers can be registered here to support alternative mod types
            var provider = new QModProvider(
                settings.PackageId,
                settings.ModFilesPath,
                settings.LibFilesPath,
                httpClientFactory.CreateClient(),
                new FileSystem(),
                modService,
                Log.Logger.ForContext(LogType.ModInstallation)
            );

            modService.RegisterProvider(provider);
            return modService;
        });

        // Configure our legacy tags
        var tagManager = new TagManager();
        tagManager.RegisterLegacyTag("modded",
            () => new PatchManifest("QuestPatcher", null) { ModloaderName = "QuestLoader" });
        tagManager.RegisterLegacyTag("BMBF.modded",
            () => new PatchManifest("BMBF", null) { ModloaderName = "QuestLoader" });

        services.AddSingleton<ITagManager>(tagManager);
        // Create a factory for IPatchBuilder, which assigns our patcher name and version
        services.AddSingleton<Func<IPatchBuilder>>(() =>
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version ?? 
                                  throw new NullReferenceException("Assembly had no version!");
            var semVersion = new SemanticVersioning.Version(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
            
            return new PatchBuilder("BMBF",
                semVersion,
                tagManager,
                new ApkSigner()
            );
        });
    }
}
