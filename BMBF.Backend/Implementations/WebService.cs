using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models;
using BMBF.Backend.Models.BPList;
using BMBF.Backend.Models.Messages;
using BMBF.Backend.Services;
using BMBF.ModManagement;
using BMBF.Resources;
using BMBF.WebServer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MimeMapping;
using Serilog;

namespace BMBF.Backend.Implementations;

public class WebService : IHostedService, IDisposable
{
    private readonly Server _server;
    private readonly BMBFSettings _settings;
    private readonly IFileProvider _webRootFileProvider;
    private readonly IBeatSaberService _beatSaberService;
    private readonly IModService _modService;
    private readonly ICoreModService _coreModService;
    private readonly IPlaylistService _playlistService;
    private readonly ISongService _songService;
    private readonly IFileImporter _fileImporter;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ISetupService _setupService;
    private readonly IAssetService _assetService;
    
    public WebService(BMBFSettings settings,
        FileProviders fileProviders,
        IBeatSaberService beatSaberService,
        IModService modService,
        ICoreModService coreModService,
        IPlaylistService playlistService, 
        ISongService songService, 
        IFileImporter fileImporter,
        JsonSerializerOptions serializerOptions,
        IMessageService messageService,
        ISetupService setupService,
        IAssetService assetService)
    {
        _settings = settings;
        _beatSaberService = beatSaberService;
        _modService = modService;
        _coreModService = coreModService;
        _playlistService = playlistService;
        _songService = songService;
        _fileImporter = fileImporter;
        _serializerOptions = serializerOptions;
        _setupService = setupService;
        _assetService = assetService;
        _webRootFileProvider = fileProviders.WebRootProvider;
        _server = new Server(settings.BindAddress, settings.BindPort);
        
        _server.Exception += OnException;
        messageService.MessageSend += OnMessageSend;
        
        Response.SerializerOptions = serializerOptions;
        
        var apiRouter = new Router();
        SetupApi(apiRouter);
        
        // First add our API endpoints
        _server.Mount("/api", apiRouter);
        
        // Make sure that / points to /index.html
        _server.Get("/", req =>
        {
            req.Path = "/index.html";
            return StaticFileHandler(req);
        });
        
        // Route remaining requests to static files
        _server.Get("*", StaticFileHandler);
    }

    protected virtual void SetupApi(Router router)
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        
        // Host endpoints (some extra android-specific endpoints are added in the BMBF project)
        router.Get("/version", _ => 
            Response.Text($"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}").Async());
        
        // Beat Saber installation endpoint
        router.Get("/install", async _ =>
        {
            var install = await _beatSaberService.GetInstallationInfoAsync();
            if (install == null)
            {
                return Response.Text("Beat Saber is not installed", 404);
            }
            return Response.Json(install);
        });
        
        
        // Mod endpoints
        router.Get("/mods", async _ => Response.Json(
            (await _modService.GetModsAsync()).Values.Select(pair => pair.mod)));
        router.Get("/mods/download/{id}", ModEndpoint(async mod => await Response.File(mod.path)));
        router.Get("/mods/cover/{id}", ModEndpoint(async mod =>
        {
            if (mod.mod.CoverImageFileName == null)
            {
                return Response.Text("Mod has no cover image", 404);
            }

            await using var coverStream = mod.mod.OpenCoverImage();
            return await Response.Stream(coverStream, MimeUtility.GetMimeMapping(mod.mod.CoverImageFileName));
        }));
        router.Post("/mods/install/{id}", ModEndpoint(async mod =>
        {
            try
            {
                await mod.mod.InstallAsync();
                return Response.Empty();
            }
            catch (InstallationException ex)
            {
                return Response.Text(ex.Message, 500);
            }
        }));
        router.Post("/mods/uninstall/{id}", ModEndpoint(async mod =>
        {
            try
            {
                await mod.mod.UninstallAsync();
                return Response.Empty();
            }
            catch (InstallationException ex)
            {
                return Response.Text(ex.Message, 500);
            }
        }));
        router.Post("/mods/installCore", async _ =>
        {
            var installResult = await _coreModService.InstallAsync(true);
            return Response.Json(installResult);
        });
        router.Post("/mods/updateStatuses", async _ =>
        {
            await _modService.UpdateModStatusesAsync();
            return Response.Empty();
        });
        
        
        // Playlist endpoints
        router.Get("/playlists", async _ => Response.Json(
            (await _playlistService.GetPlaylistsAsync())
            .Select(playlistPair => new PlaylistInfo(playlistPair.Value)))
        );
        router.Get("/playlists/cover/{id}", PlaylistEndpoint(playlist => 
            playlist.Image == null ? 
                Response.Text("Playlist has no cover image", 404).Async() 
                : new Response(playlist.Image, 200, "image/png").Async()));
        router.Put("/playlists/cover/{id}", PlaylistEndpoint((playlist, req) =>
        {
            playlist.Image = req.Body.ToArray();
            return Response.Empty().Async();
        }));
        router.Get("/playlists/songs/{id}", 
            PlaylistEndpoint(playlist => Response.Json(playlist.Songs).Async()));
        router.Put("/playlists/songs/{id}",
            PlaylistEndpoint((playlist, req) =>
            {
                playlist.Songs = req.JsonBody<ImmutableList<BPSong>>();
                return Response.Empty().Async();
            }));
        router.Get("/playlists/bplist/{id}", PlaylistEndpoint(playlist => Response.Json(playlist).Async()));
        router.Delete("/playlists/delete/{id}", PlaylistEndpoint(async playlist =>
        {
            await _playlistService.DeletePlaylistAsync(playlist.Id);
            return Response.Empty();
        }));
        router.Post("/playlists/add", async req =>
        {
            var playlistInfo = req.JsonBody<PlaylistInfo>();
            var playlist = new Playlist(
                playlistInfo.PlaylistTitle,
                playlistInfo.PlaylistAuthor,
                playlistInfo.PlaylistDescription,
                ImmutableList.Create<BPSong>()
            );
            await _playlistService.AddPlaylistAsync(playlist);
            
            return Response.Json(playlist.Id);
        });
        router.Post("/playlists/save", async _ =>
        {
            await _playlistService.SavePlaylistsAsync();
            return Response.Empty();
        });
        
        
        // Song endpoints
        router.Get("/songs", async _ => Response.Json((await _songService.GetSongsAsync()).Values));
        router.Delete("/songs/delete/{hash}", async req =>
        {
            string hash = req.Param<string>("hash");
            if (await _songService.DeleteSongAsync(hash))
            {
                return Response.Empty();
            }
            return Response.Text($"No song found with hash {hash}", 404);
        });   
        router.Get("/songs/cover/{hash}", async req =>
        {
            string hash = req.Param<string>("hash");
            if (!(await _songService.GetSongsAsync()).TryGetValue(hash, out var matching))
            {
                return Response.Text($"No song found with hash {hash}", 404);
            }

            string fullCoverPath = Path.Combine(matching.Path, matching.CoverImageFileName);
            return await Response.File(fullCoverPath);
        });
        
        
        // File importing endpoint
        router.Post("/import", async req =>
        {
            if (!req.Headers.TryGetValue("filename", out string? fileName))
            {
                return Response.Text("Cannot import file without filename", 400);
            }
            
            byte[] body = req.Body.ToArray();
            using var bodyStream = new MemoryStream(body);

            var importResult = await _fileImporter.TryImportAsync(bodyStream, fileName);
            if (importResult.Type == FileImportResultType.Failed)
            {
                Log.Error($"Failed to process file import: {importResult.Error}");
            }
            
            return Response.Json(importResult);
        });
        
        
        // Setup endpoints
        router.Get("/setup/status", async _ =>
        {
            await _setupService.LoadCurrentStatusAsync();
            if (_setupService.CurrentStatus is null)
            {
                return Response.Text("Setup is not in progress", 404);
            }
            return Response.Json(_setupService.CurrentStatus);
        });
        router.Get("/setup/moddableVersions", async _ =>
        {
            var currentCoreMods = await _assetService.GetCoreMods();
            if (currentCoreMods is null)
            {
                return Response.Json(Enumerable.Empty<string>());
            }
            return Response.Json(currentCoreMods.Value.coreMods.Keys);
        });
        router.Get("/setup/accessibleVersions", async _ =>
        {
            List<DiffInfo> diffs;
            try
            {
                diffs = await _assetService.GetDiffs();
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "Failed to fetch diffs for downgrading");
                return Response.Text("Could not fetch diffs for downgrading - this is usually caused by lack of internet",
                    404);
            }
            
            var installInfo = await _beatSaberService.GetInstallationInfoAsync();
            if (installInfo == null)
            {
                return Response.Text("Beat Saber is not installed", 400);
            }
            
            var accessibleVersions = diffs
                .Select(d => d.ToVersion) // Find all versions that a diff downgrades to
                .Distinct() // Remove duplicates
                // Only include versions where a downgrade path exists from our version
                .Where(version => diffs.FindShortestPath(installInfo.Version, version) != null);

            return Response.Json(accessibleVersions);
        });
        router.Post("/setup/begin", async _ =>
        {
            if (_setupService.CurrentStatus is not null)
            {
                return Response.Text("Setup has already started", 400);
            }

            try
            {
                await _setupService.BeginSetupAsync();
                return Response.Empty();
            }
            catch (InvalidStageException)
            {
                return Response.Text("Setup has already started", 400);
            }
            catch (InvalidOperationException)
            {
                return Response.Text("Beat Saber is not installed", 400);
            }
            
        });
        router.Post("/setup/downgrade", async req =>
        {
            List<DiffInfo> diffs;
            try
            {
                diffs = await _assetService.GetDiffs();
            }
            catch (HttpRequestException)
            {
                return Response.Text("Could not download diffs for downgrading (an internet is required)",
                    404);
            }
            
            var installInfo = await _beatSaberService.GetInstallationInfoAsync();
            if (installInfo == null)
            {
                return Response.Text("Beat Saber is not installed", 400);
            }
            string toVersion = req.JsonBody<string>();
            var path = diffs.FindShortestPath(installInfo.Version, toVersion);

            if (path == null)
            {
                return Response.Text($"No diff path found to downgrade {installInfo.Version} -> {toVersion}", 404);

            }

            try
            {
                await _setupService.DowngradeAsync(path);
                return Response.Empty();
            }
            catch (InvalidStageException)
            {
                return Response.Text("Incorrect setup stage to downgrade", 400);
            }
        });
        router.Post("/setup/patch", async _ =>
        {
            try
            {
                await _setupService.PatchAsync();
                return Response.Empty();
            }
            catch (InvalidStageException)
            {
                return Response.Text("Incorrect setup stage to patch", 400);
            }
            catch (HttpRequestException)
            {
                return Response.Text("Could not download modloader, and no modloader was built-in", 500);
            }
        });
        router.Post("/setup/triggerInstall", async _ =>
        {
            try
            {
                await _setupService.TriggerInstallAsync();
                return Response.Empty();
            }
            catch (InvalidStageException)
            {
                return Response.Text("Beat Saber must be patched and vanilla Beat Saber" +
                                     " uninstalled in order to trigger the install", 400);
            }
        });
        router.Post("/setup/triggerUninstall", async _ =>
        {
            try
            {
                await _setupService.TriggerUninstallAsync();
                return Response.Empty();
            }
            catch (InvalidStageException)
            {
                return Response.Text("Beat Saber must be patched before uninstalling", 400);
            }
        });
        router.Post("/setup/finalize", async _ =>
        {
            try
            {
                await _setupService.FinalizeSetup();
                return Response.Empty();
            }
            catch (InvalidStageException)
            {
                return Response.Text("Modded Beat Saber must be installed to finalize setup", 400);
            }
        });
        router.Post("/setup/quit", async _ =>
        {
            await _setupService.QuitSetupAsync(); 
            return Response.Empty();
        });
    }

    /// <summary>
    /// Creates an endpoint which expects an <code>id</code> parameter.
    /// This parameter will be used to find a mod by its ID and this will be passed to the underlying endpoint.
    /// If no mod exists, it will respond with 400.
    /// </summary>
    /// <param name="endpoint">Underlying endpoint taking a mod</param>
    /// <returns>A wrapper around the endpoint</returns>
    private Handler ModEndpoint(Func<(IMod mod, string path), Task<Response>> endpoint) =>
        async req =>
        {
            string modId = req.Param<string>("id");
            if (!(await _modService.GetModsAsync()).TryGetValue(modId, out var mod))
            {
                return Response.Text($"No mod exists with ID {modId}", 404);
            }

            return await endpoint(mod);
        };

    /// <summary>
    /// Creates an endpoint which expects an <code>id</code> parameter.
    /// This parameter will be used to find a playlist by its ID and this will be passed to the underlying endpoint.
    /// If no playlist exists, it will respond with 400.
    /// </summary>
    /// <param name="endpoint">Underlying endpoint taking a playlist</param>
    /// <returns>A wrapper around the endpoint</returns>
    private Handler PlaylistEndpoint(Func<Playlist, Request, Task<Response>> endpoint) => async req =>
    {
        string playlistId = req.Param<string>("id");
        if (!(await _playlistService.GetPlaylistsAsync()).TryGetValue(playlistId, out var playlist))
        {
            return Response.Text($"No playlist exists with ID {playlistId}", 404);
        }

        return await endpoint(playlist, req);
    };
    
    private Handler PlaylistEndpoint(Func<Playlist, Task<Response>> endpoint) =>
        PlaylistEndpoint((playlist, _) => endpoint(playlist));

    private void OnException(object? sender, Exception ex)
    {
        // Prefer printing the inner exception if only 1 exception is aggregated
        if (ex is AggregateException { InnerExceptions.Count: 1 } exception)
        {
            Log.Error(exception.InnerExceptions.First(),
                "Exception occurred handling request");
        }
        else
        {
            Log.Error(ex, "Multiple exceptions occurred while handing request");
        }
    }


    private void OnMessageSend(IMessage message)
    {
        byte[] messageBytes = JsonSerializer.SerializeToUtf8Bytes((object?) message, _serializerOptions);
        _server.BroadcastWebSocketMessage(messageBytes, 0, messageBytes.Length);
    }
    private async Task<Response> StaticFileHandler(Request req)
    {
        var file = _webRootFileProvider.GetFileInfo(req.Path);
        if (!file.Exists)
        {
            return Response.Text("Not found", 404);
        }

        await using var readStream = file.CreateReadStream();
        return await Response.Stream(readStream, MimeUtility.GetMimeMapping(file.Name));
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information($"Web server starting up on {_settings.BindAddress}:{_settings.BindPort}");
        _server.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping web server");
        _server.Stop();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}
