using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using Serilog;

using SongCache = System.Collections.Concurrent.ConcurrentDictionary<string, BMBF.Backend.Models.Song>;
namespace BMBF.Backend.Implementations;

/// <summary>
/// Manages the BMBF song cache
/// </summary>
public class SongService : IDisposable, ISongService
{
    // Keys are song hash, values song corresponding to hash
    private SongCache? _songs;
    private readonly string _songsPath;
    private readonly string _cachePath;
    private readonly bool _deleteDuplicateSongs;
    private readonly bool _deleteInvalidFolders;
    private readonly bool _automaticUpdates;
    private bool _disposed;

    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IFileSystemWatcher _fileSystemWatcher;

    public event EventHandler<Song>? SongAdded;
    public event EventHandler<Song>? SongRemoved;

    private readonly SemaphoreSlim _cacheUpdateLock = new(1);
    private readonly Debouncey _autoUpdateDebouncey;
    private readonly IFileSystem _io;
    private readonly int _maxConcurrentSongLoads;
    private readonly IProgressService _progressService;

    public SongService(BMBFSettings bmbfSettings,
        IFileSystem io,
        IFileSystemWatcher fileSystemWatcher,
        JsonSerializerOptions serializerOptions,
        IProgressService progressService)
    {
        _songsPath = bmbfSettings.SongsPath;
        _maxConcurrentSongLoads = bmbfSettings.MaxConcurrentSongLoads;
        _cachePath = Path.Combine(bmbfSettings.RootDataPath, bmbfSettings.SongsCacheName);
        _deleteDuplicateSongs = bmbfSettings.DeleteDuplicateSongs;
        _deleteInvalidFolders = bmbfSettings.DeleteInvalidSongs;
        _automaticUpdates = bmbfSettings.UpdateCachesAutomatically;
        _autoUpdateDebouncey = new Debouncey(bmbfSettings.SongFolderDebounceDelay);
        _autoUpdateDebouncey.Debounced += AutoUpdateDebounceyTriggered;
        _io = io;
        _fileSystemWatcher = fileSystemWatcher;
        _serializerOptions = new JsonSerializerOptions(serializerOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _progressService = progressService;
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

    public async Task<FileImportResult> ImportSongAsync(ISongProvider folderProvider, string fileName)
    {
        var song = await SongUtil.TryLoadSongInfoAsync(folderProvider, fileName);
        if (song == null)
        {
            return FileImportResult.CreateError($"{fileName} was not a valid song");
        }

        var cache = await GetSongCacheAsync();
        await _cacheUpdateLock.WaitAsync();
        try
        {
            // Early check to see if the song exists to avoid extracting the song unnecessarily
            if (cache.ContainsKey(song.Hash))
            {
                return FileImportResult.CreateError("Song already existed");
            }

            var invalidNameChars = Path.GetInvalidFileNameChars();
            var songPathBase = $"{song.SongName} ({song.SongAuthorName} - {song.LevelAuthorName})";

            var fixedPathBase = new string(songPathBase.Select(c => invalidNameChars.Contains(c) ? '_' : c).ToArray());
            var originalSavePath = Path.Combine(_songsPath, Path.GetFileNameWithoutExtension(fixedPathBase));

            song.Path = originalSavePath;
            int i = 1;
            while (_io.Directory.Exists(song.Path))
            {
                song.Path = $"{originalSavePath}_{i}";
                i++;
            }

            Log.Information($"Extracting {fileName} to {song.Path}");
            await folderProvider.CopyToAsync(song.Path, _io);

            cache[song.Hash] = song;
            Log.Information($"Song {song.SongName} import complete");
            SongAdded?.Invoke(this, song);
            return new FileImportResult
            {
                Type = FileImportResultType.Song,
                ImportedSong = song
            };
        }
        catch (Exception)
        {
            // If extracting the song fails, we need to make sure that we delete the (now garbage) directory
            if (song.Path != null)
            {
                _io.Directory.Delete(song.Path, true);
            }
            throw;
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
            _io.Directory.Delete(song.Path, true);
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

            _io.Directory.CreateDirectory(_songsPath);

            // Attempt to load the cache from BMBFData first, since it's expensive to generate
            SongCache? songs = null;
            if (_io.File.Exists(_cachePath))
            {
                try
                {
                    songs = await LoadCache();
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

    private void StartWatching()
    {
        _fileSystemWatcher.Path = _songsPath;
        _fileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName;
        _fileSystemWatcher.Deleted += OnSongDirectoryDelete;
        _fileSystemWatcher.Created += OnSongDirectoryCreate;
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private async Task<SongCache?> LoadCache()
    {
        await using var cacheStream = _io.File.OpenRead(_cachePath);
        return await JsonSerializer.DeserializeAsync<ConcurrentDictionary<string, Song>>(cacheStream, _serializerOptions);
    }

    private async Task<SongCache> GenerateOrUpdateCache(SongCache? existing, bool notify)
    {
        SongCache songs;

        IEnumerable<string> songsToLoad;
        if (existing is null)
        {
            songs = new SongCache();
            songsToLoad = _io.Directory.EnumerateDirectories(_songsPath);
        }
        else
        {
            songs = existing;
            // We collect the directories into a hashmap and then remove those that are already in the cache
            // This is to avoid a quadratic lookup later on, since we're storing songs with hashes as keys, not paths
            var songsSet = _io.Directory.EnumerateDirectories(_songsPath).ToHashSet();
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

        // TODO: Set a change tolerance?
        using var progress = _progressService.CreateProgress("Loading Songs", songsToLoad.Count());

        // If the songs haven't loaded, we need to load them for the first time now
        await Parallel.ForEachAsync(songsToLoad, new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxConcurrentSongLoads
        }, async (songPath, _) =>
        {
            await ProcessNewSongAsync(songPath, songs, notify);
            progress.ItemCompleted();
        });

        return songs;
    }

    private async Task ProcessNewSongAsync(string path, SongCache cache, bool notify)
    {
        try
        {
            Song? song = await SongUtil.TryLoadSongInfoAsync(new PhysicalSongProvider(path, _io), path);

            // If the path was a valid song
            if (song != null)
            {
                song.Path = path;
                var existingWithPath = cache.Values.FirstOrDefault(s => s.Path == path);
                if (existingWithPath != null)
                {
                    // Existing song with this path has the same hash, we can skip
                    if (existingWithPath.Hash == song.Hash)
                    {
                        return;
                    }

                    // Remove the existing song with the same path but different hash to replace with our new song
                    cache.TryRemove(existingWithPath.Hash, out _);
                    SongRemoved?.Invoke(this, existingWithPath);
                }

                // Check for an existing song with the same hash
                if (cache.TryGetValue(song.Hash, out var existingSong))
                {
                    if (_deleteDuplicateSongs)
                    {
                        _io.Directory.Delete(song.Path, true);
                    }

                    Log.Warning($"Duplicate song {(_deleteDuplicateSongs ? "deleted" : "found")}: {song.Path} ({existingSong.Path} has identical hash {song.Hash})");
                }

                Log.Information($"Song {song.SongName} (Hash: {song.Hash}) loaded");
                cache[song.Hash] = song;
                if (notify)
                {
                    SongAdded?.Invoke(this, song);
                }
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load song from {path}");
        }

        if (_deleteInvalidFolders)
        {
            Log.Warning("Deleting invalid song");
            _io.Directory.Delete(path, true);
        }
    }

    private async void AutoUpdateDebounceyTriggered(object? sender, EventArgs args)
    {
        try
        {
            Log.Debug("Song update debounced");
            await UpdateSongCacheAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process song cache update");
        }
    }

    private void OnSongDirectoryDelete(object? sender, FileSystemEventArgs args) => _autoUpdateDebouncey.Invoke();

    private void OnSongDirectoryCreate(object? sender, FileSystemEventArgs args) => _autoUpdateDebouncey.Invoke();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cacheUpdateLock.Dispose();
        _fileSystemWatcher.Dispose();
        _autoUpdateDebouncey.Dispose();

        // Save the cache
        if (_songs != null)
        {
            try
            {
                var cacheDirectory = Path.GetDirectoryName(_cachePath);
                if (cacheDirectory != null) _io.Directory.CreateDirectory(cacheDirectory);
                if (_io.File.Exists(_cachePath)) _io.File.Delete(_cachePath);

                using var cacheStream = _io.File.OpenWrite(_cachePath);
                cacheStream.Position = 0;
                JsonSerializer.Serialize(cacheStream, _songs, _serializerOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save song cache");
            }
        }
    }
}
