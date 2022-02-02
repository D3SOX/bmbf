using System;
using System.IO.Abstractions;
using System.Net.Http;
using System.Reflection;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Services;
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
        services.AddSingleton<IAssetService, AssetService>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IBeatSaverService, BeatSaverService>();
        services.AddTransient<IFileSystemWatcher>(s =>
            (s.GetService<IFileSystem>() ?? throw new NullReferenceException($"No {nameof(IFileSystem)} configured"))
            .FileSystemWatcher.CreateNew());

        services.AddHttpClient<IBeatSaverService, BeatSaverService>(client =>
        {
            ConfigureDefaults(client);
            client.BaseAddress = settings.BeatSaverBaseUri;
        });
        services.AddHttpClient<IAssetService, AssetService>(ConfigureDefaults);
        services.AddHttpClient();

        services.AddSingleton(settings);
        services.AddSingleton(resources);
        services.AddSingleton(assetFileProvider);

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

        services.AddSingleton<ITagManager>(tagManager);
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
