using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Configuration;
using BMBF.Models;
using BMBF.Services;
using BMBF.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using PlaylistCache = System.Collections.Concurrent.ConcurrentDictionary<string, BMBF.Models.Playlist>;

namespace BMBF.Implementations;

public class PlaylistService : IPlaylistService, IDisposable
{
    public event EventHandler<Playlist>? PlaylistAdded;
    public event EventHandler<Playlist>? PlaylistDeleted;

    private PlaylistCache? _cache;
    private readonly SemaphoreSlim _cacheUpdateLock = new(1);
    private readonly JsonSerializer _serializer = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private readonly string _playlistsPath;
    private readonly bool _automaticUpdates;

    private readonly FileSystemWatcher _fileSystemWatcher = new();
    private readonly Debouncey _autoUpdateDebouncey;

    private bool _disposed;
        
    public PlaylistService(BMBFSettings settings)
    {
        _playlistsPath = settings.PlaylistsPath;
        _automaticUpdates = settings.UpdateCachesAutomatically;
        _autoUpdateDebouncey = new Debouncey(settings.PlaylistFolderDebounceDelay);
        _autoUpdateDebouncey.Debounced += AutoUpdateDebounceyTriggered;
    }

    public async Task<string> AddPlaylistAsync(Playlist playlist)
    {
        var playlists = await GetCacheAsync();
        playlist.IsPendingSave = true;
        return AddPlaylist(playlist, playlists, playlist.PlaylistTitle);
    }
        
    private string AddPlaylist(Playlist playlist, PlaylistCache cache, string idSuggestion)
    {
        var originalId = new string(idSuggestion.Select(c => Playlist.LegalIdCharacters.Contains(c) ? c : '_').ToArray());
        playlist.Id = originalId;
                    
        // Find a playlist ID that isn't used yet
        int i = 1;
        while (!cache.TryAdd(playlist.Id, playlist))
        {
            playlist.Id = $"{originalId}_{i}";
            i++;
        }
        return playlist.Id;
    }

    public async Task UpdatePlaylistCacheAsync()
    {
        // Load the cache if not already loaded
        if (_cache == null)
        {
            await GetCacheAsync();
            return;
        }

        await _cacheUpdateLock.WaitAsync();
        try
        {
            await UpdateCacheAsync(_cache, true);
        }
        finally
        {
            _cacheUpdateLock.Release();
        }
    }

    public async ValueTask<IReadOnlyDictionary<string, Playlist>> GetPlaylistsAsync()
    {
        return await GetCacheAsync();
    }

    public async Task SavePlaylistsAsync()
    {
        if (_cache == null)
        {
            Log.Information("No playlists to save");
            return;
        }
            
        await _cacheUpdateLock.WaitAsync().ConfigureAwait(false);
        try
        {
            Log.Information("Saving playlists");
            foreach (var playlistPair in _cache)
            {
                var playlist = playlistPair.Value;
                if(!playlist.IsPendingSave) { continue; }
                Log.Debug($"Saving playlist {playlistPair.Key}");

                try
                {
                    // Find a path for the playlist if there isn't one already
                    if (playlist.LoadedFrom == null)
                    {
                        string newPath = Path.Combine(_playlistsPath, playlistPair.Key + ".bplist");
                        int i = 1;
                        while (File.Exists(newPath))
                        {
                            newPath = Path.Combine(_playlistsPath, playlistPair.Key + "_" + i + ".bplist");
                            i++;
                        }
                        playlist.LoadedFrom = newPath;
                    }
                        
                    using var playlistStream = new StreamWriter(playlist.LoadedFrom);
                    using var jsonWriter = new JsonTextWriter(playlistStream);
                    await Task.Run(() => _serializer.Serialize(jsonWriter, playlist)).ConfigureAwait(false);
                    playlist.IsPendingSave = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to save playlist {playlistPair.Key}");
                }
            }
        }
        finally
        {
            _cacheUpdateLock.Release();
        }
    }
        
    public async Task<bool> DeletePlaylistAsync(string playlistId)
    {
        PlaylistCache playlists = await GetCacheAsync();

        if (playlists.TryRemove(playlistId, out var playlist))
        {
            // If the playlist was actually loaded from a file, then delete it
            if (playlist.LoadedFrom != null)
            {
                File.Delete(playlist.LoadedFrom);
            }
            PlaylistDeleted?.Invoke(this, playlist);
            return true;
        }
        return false;
    }

    private async ValueTask<PlaylistCache> GetCacheAsync()
    {
        if (_cache != null) { return _cache; }

        await _cacheUpdateLock.WaitAsync();
        try
        {
            if (_cache != null)
            {
                return _cache;
            }

            Directory.CreateDirectory(_playlistsPath);

            // We wait to assign the cache field until the cache is fully loaded
            var cache = new PlaylistCache();
            await UpdateCacheAsync(cache, false);

            _cache = cache;
            if (_automaticUpdates)
            {
                StartWatching();
            }
        }
        finally
        {
            _cacheUpdateLock.Release();
        }

        return _cache;
    }
    
    private void StartWatching()
    {
        _fileSystemWatcher.Path = _playlistsPath;
        _fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        _fileSystemWatcher.Filter = "*.*";
        _fileSystemWatcher.Deleted += OnPlaylistFileDeleted;
        _fileSystemWatcher.Changed += OnPlaylistFileChanged;
        _fileSystemWatcher.Renamed += OnPlaylistFileRenamed;
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private async Task UpdateCacheAsync(PlaylistCache cache, bool notify)
    {
        foreach(var entry in cache)
        {
            if (!File.Exists(entry.Value.LoadedFrom))
            {
                Log.Information($"Playlist {entry.Key} deleted");
                cache.Remove(entry.Key, out _);
                if (notify)
                {
                    PlaylistDeleted?.Invoke(this, entry.Value);
                }
            } 
        }
            
        foreach (string playlistPath in Directory.EnumerateFiles(_playlistsPath))
        {
            await ProcessNewPlaylistAsync(playlistPath, cache, notify);
        }
    }

    private async Task ProcessNewPlaylistAsync(string path, PlaylistCache cache, bool notify, bool logFailToRead = true)
    {
        try
        {
            var existing = cache.FirstOrDefault(p => p.Value.LoadedFrom == path).Value;
            // We can skip loading if:
            // - The existing playlist is pending save - changes made in BMBF are prioritised over those on disk
            // - The playlist file was modified at the same time (or further before) the playlist was loaded
            var lastWriteTime = File.GetLastWriteTimeUtc(path);
            if (existing != null && (existing.IsPendingSave || lastWriteTime <= existing.LastLoadTime))
            {
                return;
            }

            // FileShare.Read is used here, since we don't want to attempt to open a playlist while it is still being written
            await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            using var streamReader = new StreamReader(fileStream);
            using var jsonReader = new JsonTextReader(streamReader);
            // No async with newtonsoft :/
            var playlist = await Task.Run(() => _serializer.Deserialize<Playlist>(jsonReader));
            if (playlist == null)
            {
                Log.Warning($"Deserialized playlist from {path} was null");
                return;
            }

            playlist.LoadedFrom = path;
            playlist.LastLoadTime = lastWriteTime;
            playlist.IsPendingSave = false;

            // TODO: What to do with BPSongs that don't exist in the song cache?

            // If a playlist with this path already exists, we need to copy over the new values
            if (existing != null)
            {
                existing.SetPlaylistInfo(new PlaylistInfo(playlist));
                existing.Image = playlist.Image;
                existing.Songs = playlist.Songs;
                existing.LastLoadTime = playlist.LastLoadTime;
                existing.IsPendingSave = false;
                Log.Information($"Playlist ({path}) was reloaded");
            }
            else
            {
                string playlistId = AddPlaylist(playlist, cache, Path.GetFileNameWithoutExtension(path));
                Log.Information($"Playlist ({playlistId}) loaded from {path}");
                if (notify)
                {
                    PlaylistAdded?.Invoke(this, playlist);
                }
            }
        }
        catch (IOException)
        {
            if (logFailToRead)
            {
                Log.Warning($"Failed to read playlist from {path} due to an IO error");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load playlist {path}");
        }
    }
    
    private async void AutoUpdateDebounceyTriggered(object? sender, EventArgs args)
    {
        try
        {
            Log.Debug("Playlist update debounced");
            await UpdatePlaylistCacheAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process song cache update");
        }
    }

    private void OnPlaylistFileChanged(object? sender, FileSystemEventArgs args) => _autoUpdateDebouncey.Invoke();

    private void OnPlaylistFileDeleted(object? sender, FileSystemEventArgs args) => _autoUpdateDebouncey.Invoke();

    private void OnPlaylistFileRenamed(object? sender, FileSystemEventArgs args) => _autoUpdateDebouncey.Invoke();
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
            
        SavePlaylistsAsync().Wait();

        _cacheUpdateLock.Dispose();
        _fileSystemWatcher.Dispose();
        _autoUpdateDebouncey.Dispose();
    }
}