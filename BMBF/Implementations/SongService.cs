#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using BMBF.Models;
using BMBF.Services;
using BMBF.Util;
using Newtonsoft.Json;
using Serilog;

using SongCache = System.Collections.Concurrent.ConcurrentDictionary<string, BMBF.Models.Song>;
namespace BMBF.Implementations
{
    /// <summary>
    /// Manages the BMBF song cache
    /// </summary>
    public class SongService : FileObserver, IDisposable, ISongService
    {
        /// <summary>
        /// Time between a song directory being created and attempting to load that directory as a song, in milliseconds.
        /// </summary>
        private const int SongLoadDelay = 5000;
        
        
        // Keys are song hash, values song corresponding to hash
        private SongCache? _songs;
        private readonly string _songsPath;
        private readonly string _cachePath;
        private readonly bool _deleteDuplicateSongs;
        private readonly bool _deleteInvalidFolders;
        private readonly bool _automaticUpdates;
        
        private readonly JsonSerializer _jsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public event EventHandler<Song>? SongAdded; 
        public event EventHandler<Song>? SongRemoved;

        private readonly SemaphoreSlim _cacheUpdateLock = new SemaphoreSlim(1);

        public SongService(BMBFSettings bmbfSettings) : base(bmbfSettings.SongsPath, FileObserverEvents.Create | FileObserverEvents.Delete)
        {
            _songsPath = bmbfSettings.SongsPath;
            _cachePath = Path.Combine(bmbfSettings.RootDataPath, bmbfSettings.SongsCacheName);
            _deleteDuplicateSongs = bmbfSettings.DeleteDuplicateSongs;
            _deleteInvalidFolders = bmbfSettings.DeleteInvalidSongs;
            _automaticUpdates = bmbfSettings.UpdateCacheAutomatically;
        }

        public async Task UpdateSongCacheAsync()
        {
            // If songs are yet to be loaded, we can just load them
            if (_songs == null)
            {
                await GetSongCacheAsync();
                return;
            }
            
            await _cacheUpdateLock.WaitAsync();
            try
            {
                _songs = await GenerateOrUpdateCache(_songs, true);
            }
            finally
            {
                _cacheUpdateLock.Release();
            }
        }

        public async ValueTask<IReadOnlyDictionary<string, Song>> GetSongsAsync()
        {
            return await GetSongCacheAsync();
        }
        
        public async Task<bool> DeleteSongAsync(string hash)
        {
            SongCache songs = await GetSongCacheAsync();
            if (songs.TryRemove(hash, out var song))
            {
                // Note that if multiple songs existed with this hash, and DeleteDuplicateSongs was false, then there may still be another folder with this hash
                // This folder will be loaded next time BMBF is restarted
                Directory.Delete(song.Path, true);
                Log.Information($"Song {song.SongName}: {song.Hash} deleted");
                SongRemoved?.Invoke(this, song);
                return true;
            }

            return false;
        }

        private async ValueTask<SongCache> GetSongCacheAsync()
        {
            // If songs have already been loaded, we can return them now
            if (_songs != null)
            {
                return _songs;
            }
            
            await _cacheUpdateLock.WaitAsync(); // Avoid another call beginning cache loading while we start
            try
            {
                // If the cache was loaded by whoever was locking the load lock, we can return it now
                if (_songs != null)
                {
                    return _songs;
                }
                
                Directory.CreateDirectory(_songsPath);

                // Attempt to load the cache from BMBFData first, since it's expensive to generate
                SongCache? songs = null;
                if (File.Exists(_cachePath))
                {
                    try
                    {
                        songs = LoadCache();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to load existing cache from disk. Generating a new cache instead");
                    }
                }

                _songs = await GenerateOrUpdateCache(songs, false); // Do not notify on initial cache load

                if (_automaticUpdates)
                {
                    StartWatching();
                }
                return _songs;
            }
            finally
            {
                _cacheUpdateLock.Release();
            }
        }

        private SongCache LoadCache()
        {
            using var reader = new StreamReader(_cachePath);
            using var jsonReader = new JsonTextReader(reader);
            return _jsonSerializer.Deserialize<ConcurrentDictionary<string, Song>>(jsonReader);
        }

        private async Task<SongCache> GenerateOrUpdateCache(SongCache? existing, bool notify)
        {
            SongCache songs;

            IEnumerable<string> songsToLoad;
            if (existing is null)
            {
                songs = new SongCache();
                songsToLoad = Directory.EnumerateDirectories(_songsPath);
            }
            else
            {
                songs = existing;
                // We collect the directories into a hashmap and then remove those that are already in the cache
                // This is to avoid a quadratic lookup later on, since we're storing songs with hashes as keys, not paths
                var songsSet = Directory.EnumerateDirectories(_songsPath).ToHashSet();
                foreach (var existingPair in existing)
                {
                    if (!songsSet.Remove(existingPair.Value.Path))
                    {

                        // This song was deleted on disk, as it existed in the songs but its path wasn't in the directories anymore
                        existing.Remove(existingPair.Key, out _);
                        Log.Information($"Song {existingPair.Key} removed from cache");
                        if (notify)
                        {
                            SongRemoved?.Invoke(this, existingPair.Value);
                        }
                    }
                }
                songsToLoad = songsSet;
            }

            // If the songs haven't loaded, we need to load them for the first time now
            Log.Information($"Loading songs from {_songsPath}");
            foreach (string songPath in songsToLoad)
            {
               await ProcessNewSongAsync(songPath, songs, notify);
            }

            return songs;
        }
        
        private async Task ProcessNewSongAsync(string path, SongCache cache, bool notify)
        {
            try
            {
                Song? song = await SongUtil.TryLoadSongInfoAsync(path);
                // If the path was a valid song
                if (song != null)
                {
                    // Check for an existing song with the same hash
                    if (cache.TryGetValue(song.Hash, out var existingSong))
                    {
                        if (_deleteDuplicateSongs)
                        {
                            Directory.Delete(song.Path, true);
                        }

                        Log.Warning($"Duplicate song {(_deleteDuplicateSongs ? "deleted" : "found")}: {song.Path} ({existingSong.Path} has identical hash {song.Hash})");
                    }
                    
                    Log.Information($"Song {song.SongName} (Hash: {song.Hash}) loaded");
                    cache[song.Hash] = song;
                    if (notify)
                    {
                        SongAdded?.Invoke(this, song);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load song from {path}");
            }
            
            if (_deleteInvalidFolders)
            {
                Log.Warning("Deleting invalid song");
                Directory.Delete(path, true);
            }
        }

        public override async void OnEvent(FileObserverEvents evnt, string? name)
        {
            try
            {
                if (name == null || _songs == null) { return; }

                string fullPath = Path.Combine(_songsPath, name);
                if (evnt.HasFlag(FileObserverEvents.Create) && File.GetAttributes(fullPath).HasFlag(FileAttributes.Directory))
                {
                    // At this point, a song directory has just been created
                    // Chances are, the files of the song haven't actually been added yet
                    // We will wait for a configured period of time before attempting to load as a song
                    // TODO: A better check for this. Wait until no activity in the directory for a certain period of time
                    await Task.Delay(SongLoadDelay);
                    
                    await ProcessNewSongAsync(fullPath, _songs, true);
                }
                if (evnt.HasFlag(FileObserverEvents.Delete))
                {
                    Song? removed = null;
                    foreach(Song song in _songs.Values)
                    {
                        if (song.Path == fullPath)
                        {
                            Log.Information($"Song {song.SongName} (Hash: {song.Hash}) was deleted");
                            removed = song;
                        }
                    }

                    if (removed != null)
                    {
                        _songs.Remove(removed.Hash, out _);
                        SongRemoved?.Invoke(this, removed);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to process song cache update");
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            // Save the cache
            if (_songs != null)
            {
                try
                {
                    using var writer = new StreamWriter(_cachePath);
                    using var jsonWriter = new JsonTextWriter(writer);
                    _jsonSerializer.Serialize(jsonWriter, _songs);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to save song cache");
                }
            }
        }
    }
}