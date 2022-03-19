using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models;
using BMBF.Backend.Models.BPList;
using BMBF.Backend.Services;
using Serilog;
using SongFeedReaders.Feeds;
using SongFeedReaders.Feeds.BeastSaber;

namespace BMBF.Backend.Implementations;

public class SyncSaberService : ISyncSaberService
{
    private readonly SemaphoreSlim _configLock = new(1);
    private readonly string _configPath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IPlaylistService _playlistService;
    private readonly IFileImporter _fileImporter;
    private readonly IProgressService _progressService;
    private readonly Dictionary<FeedType, (IFeed feed, IFeedSettings settings)> _feedReaders = new();
    private SyncSaberConfig? _config;

    public SyncSaberService(BMBFSettings settings,
        JsonSerializerOptions serializerOptions,
        IPlaylistService playlistService,
        IProgressService progressService,
        IFileImporter fileImporter,
        IEnumerable<IFeed> feeds,
        IEnumerable<IFeedSettings> feedSettings)
    {
        // Match up the feeds with their feed settings
        var feedsById = feeds.ToDictionary(f => f.FeedId, f => f);
        var feedSettingsById = feedSettings.ToDictionary(s => s.FeedId, s => s);
        foreach (var feedType in Enum.GetValues<FeedType>())
        {
            string feedId = feedType.GetFeedId();
            _feedReaders[feedType] = (feedsById[feedId], feedSettingsById[feedId]);
        }
        
        _serializerOptions = serializerOptions;
        _playlistService = playlistService;
        _progressService = progressService;
        _fileImporter = fileImporter;
        _configPath = Path.Combine(settings.RootDataPath, settings.SyncSaberConfigName);
    }

    public async Task<SyncSaberConfig> GetConfig()
    {
        if (_config != null)
        {
            return _config;
        }
        
        await _configLock.WaitAsync();
        try
        {
            if (_config != null)
            {
                return _config;
            }
            await using var cfgStream = File.OpenRead(_configPath);
            _config = await JsonSerializer.DeserializeAsync<SyncSaberConfig>(cfgStream, _serializerOptions)
                      ?? throw new NullReferenceException("Deserialized config was null");
        }
        catch(Exception ex)
        {
            if (ex is FileNotFoundException)
            {
                Log.Debug("SyncSaber config not found, generating new");
            }
            else
            {
                Log.Error(ex, "Failed to load SyncSaber config, generating new");
            }

            _config = new SyncSaberConfig();
            SetupConfig(_config); // Add sinks
            await SaveConfigInternal();
        }
        finally
        {
            _configLock.Release();
        }
        return _config;
    }

    public async Task OverwriteConfig(SyncSaberConfig cfg)
    {
        await _configLock.WaitAsync();
        try
        {
            SetupConfig(cfg); // Sanitise config
            _config = cfg;
            await SaveConfigInternal();
        }
        finally
        {
            _configLock.Release();
        }
    }

    private void SetupConfig(SyncSaberConfig config)
    {
        foreach (var feed in Enum.GetValues<FeedType>())
        {
            if (!config.Feeds.ContainsKey(feed))
            {
                config.Feeds[feed] = new FeedSettings
                {
                    SongsToSync = 10,
                    Enabled = false
                };
            }
        }
    }

    private async Task SaveConfigInternal()
    {
        if (File.Exists(_configPath))
        {
            File.Delete(_configPath);
        }

        await using var cfgStream = File.OpenWrite(_configPath);
        await JsonSerializer.SerializeAsync(cfgStream, _config, _serializerOptions);
    }

    public async Task Sync()
    {
        var playlists = await _playlistService.GetPlaylistsAsync();

        var config = await GetConfig();

        using var feedProgress = _progressService.CreateProgress("(Sync) Updating playlists", config.Feeds.Count);
        foreach (var (type, settings) in config.Feeds)
        {
            if (!settings.Enabled)
            {
                continue;
            }

            var (reader, readerSettings) = _feedReaders[type];
            if (readerSettings is BeastSaberBookmarksSettings bookmarksSettings)
            {
                if (config.BeastSaberUsername == null)
                {
                    Log.Warning("Attempted to sync bookmarks without BeastSaber username");
                    continue; // Cannot sync bookmarks without username
                }

                bookmarksSettings.Username = config.BeastSaberUsername;
            }

            reader.TryAssignSettings(readerSettings); // Make sure we add the required settings
            if (!reader.Initialized)
            {
                await reader.InitializeAsync(default);
            }

            Log.Information($"Fetching songs from feed {type}");
            var songs = new List<BPSong>();
            var enumerator = reader.GetAsyncEnumerator();
            int pages = 0;
            while(songs.Count < settings.SongsToSync) // Continue reading pages until song count is satisfied
            {
                Log.Debug($"Fetching page {pages}");
                pages++;
                    
                var result = await enumerator.MoveNextAsync();
                if (!result.Songs().Any())
                {
                    Log.Warning($"Could not sync {settings.SongsToSync} songs from {type}: only {songs.Count} songs existed");
                    break;
                }
                    
                var pageSongs = result.Songs().DistinctBy(s => s.Hash);
                foreach (var scrapedSong in pageSongs)
                {
                    // Song hash is required in playlists, so we must skip songs without a hash
                    if (scrapedSong.Hash == null)
                    {
                        continue;
                    }
                        
                    songs.Add(new BPSong(scrapedSong.Hash.ToUpper(), scrapedSong.Name, scrapedSong.Key));
                    if (songs.Count >= settings.SongsToSync)
                    {
                        break;
                    }
                }
            }

            // Create a playlist to manage this feed
            var playlist = new Playlist(
                type.GetDisplayName(),
                "Unicorns",
                "Sync Saber playlist",
                ImmutableList.Create(songs.ToArray()),
                syncSaberFeed: type
            );

            // Download the new songs from the feed (concurrently)
            await _fileImporter.DownloadSongs(playlist, $"(Sync) Downloading songs from {type.GetDisplayName()}");

            // Delete existing playlists with the same feed
            foreach (var existing in playlists.Values.Where(p => p.SyncSaberFeed == type))
            {
                await _playlistService.DeletePlaylistAsync(existing.Id);
            }

            // Add our synced playlist
            await _playlistService.AddPlaylistAsync(playlist);
            feedProgress.Completed++;
        }
    }
}
