using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using BMBF.Models;
using BMBF.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using PlaylistCache = System.Collections.Concurrent.ConcurrentDictionary<string, BMBF.Models.Playlist>;

namespace BMBF.Implementations
{
    public class PlaylistService : FileObserver, IPlaylistService, IDisposable
    {
        public event EventHandler<Playlist>? PlaylistAdded;
        public event EventHandler<Playlist>? PlaylistDeleted;

        private PlaylistCache? _cache;
        private readonly SemaphoreSlim _cacheUpdateLock = new SemaphoreSlim(1);
        private readonly JsonSerializer _serializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly string _playlistsPath;
        private readonly bool _automaticUpdates;
        
        public PlaylistService(BMBFSettings settings) : base(settings.PlaylistsPath, FileObserverEvents.CloseWrite | FileObserverEvents.Delete)
        {
            _playlistsPath = settings.PlaylistsPath;
            _automaticUpdates = settings.UpdateCacheAutomatically;
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
            Log.Information("Saving playlists");
            foreach (var playlist in await GetPlaylistsAsync())
            {
                if(!playlist.Value.IsPendingSave) { continue; }
                Log.Debug($"Saving playlist {playlist.Key}");

                try
                {
                    using var playlistStream = new StreamWriter(playlist.Key);
                    using var jsonWriter = new JsonTextWriter(playlistStream);
                    _serializer.Serialize(jsonWriter, playlist);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to save playlist {playlist.Key}");
                }
            }
        }
        
        public async Task<bool> DeletePlaylistAsync(string playlistPath)
        {
            PlaylistCache playlists = await GetCacheAsync();
            if (playlists.TryRemove(playlistPath, out var playlist))
            {
                File.Delete(playlistPath);
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

        private async Task UpdateCacheAsync(PlaylistCache cache, bool notify)
        {
            foreach(var entry in cache)
            {
                if (!File.Exists(entry.Key))
                {
                    Log.Information($"Playlist {entry.Value.PlaylistTitle} deleted");
                    cache.Remove(entry.Key, out _);
                    if (notify)
                    {
                        PlaylistDeleted?.Invoke(this, entry.Value);
                    }
                } 
            }
            
            foreach (string playlistPath in Directory.EnumerateFiles(_playlistsPath))
            {
                // Don't bother reloading the playlist if its last load time is newer than the last write time
                if (cache.TryGetValue(playlistPath, out var existing))
                {
                    if (File.GetLastWriteTimeUtc(playlistPath) < existing.LastLoadTime)
                    {
                        continue;
                    }
                }
                
                await ProcessNewPlaylistAsync(playlistPath, cache, notify);
            }
        }

        private async Task ProcessNewPlaylistAsync(string path, PlaylistCache cache, bool notify)
        {
            try
            {
                using var streamReader = new StreamReader(path);
                using var jsonReader = new JsonTextReader(streamReader);
                var playlist = _serializer.Deserialize<Playlist>(jsonReader);
                if (playlist == null)
                {
                    Log.Warning($"Deserialized playlist from {path} was null");
                    return;
                }
                
                playlist.PlaylistId = new string(Path.GetFileNameWithoutExtension(path).Select(c => Char.IsWhiteSpace(c) ? '_' : c).ToArray());
                
                // TODO: What to do with BPSongs that don't exist in the song cache?

                // If a playlist with this path already exists, we need to copy over the new values
                if (cache.TryGetValue(path, out var existing))
                {
                    existing.PlaylistTitle = playlist.PlaylistTitle;
                    existing.Image = playlist.Image;
                    existing.PlaylistDescription = playlist.PlaylistDescription;
                    existing.PlaylistAuthor = playlist.PlaylistAuthor;
                    existing.Songs = playlist.Songs;
                    existing.LastLoadTime = playlist.LastLoadTime;
                    existing.IsPendingSave = false;
                    Log.Information($"Playlist ({path}) was reloaded");
                }
                else
                {
                    cache[path] = playlist;
                    Log.Information($"Playlist ({playlist.PlaylistTitle}) loaded from {path}");
                    if (notify)
                    {
                        PlaylistAdded?.Invoke(this, playlist);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load playlist {Path.GetFileName(path)}");
            }
        }

        public override async void OnEvent(FileObserverEvents evnt, string? name)
        {
            try
            {
                if (name == null || _cache == null) { return; }

                string fullPath = Path.Combine(_playlistsPath, name);
                if (evnt.HasFlag(FileObserverEvents.Delete))
                {
                    if (_cache.TryRemove(fullPath, out var playlist))
                    {
                        Log.Information($"Playlist {fullPath} deleted");
                        PlaylistDeleted?.Invoke(this, playlist);
                    }
                }

                // If a playlist file has just been closed for writing, then we need to add it (if new), or process any changes and fire events accordingly
                if (evnt.HasFlag(FileObserverEvents.CloseWrite) && !File.GetAttributes(fullPath).HasFlag(FileAttributes.Directory))
                {
                    await ProcessNewPlaylistAsync(fullPath, _cache, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to process playlist cache update");
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            SavePlaylistsAsync().Wait();
        }
    }
}