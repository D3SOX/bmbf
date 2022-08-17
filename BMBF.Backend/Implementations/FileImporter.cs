using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Backend.Models.BPList;
using BMBF.Backend.Util;
using BMBF.ModManagement;
using BMBF.Resources;
using Serilog;

namespace BMBF.Backend.Implementations;

public class FileImporter : IFileImporter
{
    private readonly ISongService _songService;
    private readonly IPlaylistService _playlistService;
    private readonly IBeatSaverService _beatSaverService;
    private readonly BMBFSettings _bmbfSettings;
    private readonly IModService _modService;
    private readonly IAssetService _assetService;
    private readonly IFileSystem _io;
    private readonly IProgressService _progressService;

    private FileExtensions? _extensions;

    public FileImporter(ISongService songService,
        IPlaylistService playlistService,
        IBeatSaverService beatSaverService,
        BMBFSettings bmbfSettings,
        IModService modService,
        IAssetService assetService,
        IFileSystem io, IProgressService progressService)
    {
        _songService = songService;
        _playlistService = playlistService;
        _beatSaverService = beatSaverService;
        _bmbfSettings = bmbfSettings;
        _modService = modService;
        _assetService = assetService;
        _io = io;
        _progressService = progressService;
    }

    private async Task<string?> TryImportPlaylistAsync(Stream stream, string fileName)
    {
        Playlist playlist;
        try
        {
            playlist = await stream.ReadAsCamelCaseJsonAsync<Playlist>();
        }
        catch (JsonException ex)
        {
            Log.Error($"Could not parse as playlist: {ex.Message}");
            return null;
        }

        string playlistId = await _playlistService.AddPlaylistAsync(playlist);
        
        await DownloadSongs(playlist, $"Downloading songs from {playlist.PlaylistTitle}");
        Log.Information($"Playlist file {fileName} imported as {playlistId}");
        return playlistId;
    }

    private async Task CopyFile(Stream stream, string fileName, string saveDirectory)
    {
        _io.Directory.CreateDirectory(saveDirectory);
        var outputPath = Path.Combine(saveDirectory, fileName);
        if (_io.File.Exists(outputPath)) _io.File.Delete(outputPath);

        Log.Information($"Copied {fileName} to {saveDirectory}");
        await using var outputStream = _io.File.OpenWrite(outputPath);
        await stream.CopyToAsync(outputStream);
    }

    public async Task<FileImportResult> TryImportAsync(Stream stream, string fileName)
    {
        Log.Information($"Importing {fileName}");

        var extension = Path.GetExtension(fileName).ToLowerInvariant().Substring(1);

        if (extension == "zip")
        {
            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                return await _songService.ImportSongAsync(new ArchiveSongProvider(archive), fileName);
            }
            catch (InvalidDataException)
            {
                return FileImportResult.CreateError("The given song was not a valid ZIP archive");
            }
        }

        if (_extensions == null)
        {
            try
            {
                _extensions = await _assetService.GetExtensions();
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "No extensions were built-in, and loading extensions from BMBF resources failed");
                return FileImportResult.CreateError("No copy extensions were built in to the APK, and downloading them failed");
            }
        }

        // Attempt to import as a mod config. This only applies if the config's filename matches the ID of a loaded mod
        if (_extensions.ConfigExtensions.Contains(extension))
        {
            string modId = Path.GetFileNameWithoutExtension(fileName);
            bool modExistsWithId = (await _modService.GetModsAsync()).ContainsKey(modId);
            if (modExistsWithId)
            {
                await CopyFile(stream, fileName, _bmbfSettings.ConfigsPath);
                Log.Information($"{fileName} imported as config for {modId}");

                return new FileImportResult
                {
                    Type = FileImportResultType.Config,
                    ConfigModId = modId
                };
            }
        }

        if (_extensions.PlaylistExtensions.Contains(extension))
        {
            string? playlistId = await TryImportPlaylistAsync(stream, fileName);
            if (playlistId != null)
            {
                return new FileImportResult
                {
                    Type = FileImportResultType.Playlist,
                    ImportedPlaylistId = playlistId
                };
            }

            return FileImportResult.CreateError($"{fileName} was not a valid playlist");
        }

        // At this point, we may need to attempt an import multiple times, therefore we copy to a memory stream
        if (!stream.CanSeek)
        {
            var memStream = new MemoryStream();
            await stream.CopyToAsync(memStream);
            stream = memStream;
        }
        // Make sure that the (possibly Memory) Stream gets disposed at the end of this scope
        await using var _ = stream;
        
        stream.Position = 0;

        // Now we'll attempt to import the file as a mod
        var result = await _modService.TryImportModAsync(stream, fileName);
        if (result != null)
        {
            return result;
        }

        // If the result was null, the stream/filename didn't constitute a mod
        // Therefore, we will rewind the stream and attempt to import as a copy extension
        stream.Position = 0;

        (IMod? mod, string destination)? selectedCopy = null;

        if (_extensions.CopyExtensions.TryGetValue(extension, out string? destination))
        {
            selectedCopy = (null, destination);
        }

        // Search through the installed mods to see if any can process this file type
        var mods = (await _modService.GetModsAsync()).Values;
        foreach (var modPair in mods)
        {
            if (modPair.mod.CopyExtensions.TryGetValue(extension, out destination))
            {
                if (selectedCopy == null)
                {
                    Log.Debug($"Found mod {modPair.mod.Id} which can process {extension} files");
                    selectedCopy = (modPair.mod, destination);
                }
                else
                {
                    // For now, we will just fail if multiple mods/builtin extensions match the file extension
                    return FileImportResult.CreateError($"Multiple file copy destinations found for {extension}");
                }
            }
        }
        if (selectedCopy != null)
        {
            var (mod, copyDest) = selectedCopy.Value;
            await CopyFile(stream, fileName, copyDest);
            return new FileImportResult
            {
                Type = FileImportResultType.FileCopy,
                FileCopyInfo = new FileCopyInfo(copyDest, mod?.Id)
            };
        }

        return FileImportResult.CreateError($"Unrecognised file type .{extension}");
    }

    public async Task<FileImportResult> ImportAsync(Stream stream, string fileName)
    {
        var result = await TryImportAsync(stream, fileName);
        if (result.Type == FileImportResultType.Failed)
        {
            throw new ImportException(result.Error);
        }
        return result;
    }

    public async Task DownloadSongs(Playlist playlist, string? progressName = null, IProgress? parentProgress = null)
    {
        // Install any missing songs in the playlist
        var songs = await _songService.GetSongsAsync();
        var missingSongs = new List<BPSong>();
        foreach (var song in playlist.Songs)
        {
            song.Hash = song.Hash.ToUpper(); // Verify hash case now
            if (!songs.ContainsKey(song.Hash))
            {
                missingSongs.Add(song);
            }
        }
        
        if (missingSongs.Count == 0)
        {
            Log.Information("All songs in the playlist were already downloaded");
            return;
        }
        Log.Information($"Found {missingSongs.Count} songs that will need to be installed");

        using var progress = progressName == null ? null : _progressService.CreateProgress(progressName, missingSongs.Count, parent: parentProgress);

        await Parallel.ForEachAsync(missingSongs, new ParallelOptions
        {
            MaxDegreeOfParallelism = _bmbfSettings.MaxConcurrentDownloads
        }, async (bpSong, _) =>
        {
            Log.Information($"Downloading song {bpSong.SongName ?? bpSong.Hash}");
            try
            {
                await using var mapStream = bpSong.Key == null ? await _beatSaverService.DownloadSongByHash(bpSong.Hash) : await _beatSaverService.DownloadSongByKey(bpSong.Key);
                if (mapStream == null)
                {
                    Log.Warning($"No map with hash {bpSong.Hash} or with key {bpSong.Key} found on BeatSaver");
                    return;
                }

                using var archive = new ZipArchive(mapStream, ZipArchiveMode.Read);
                var importResult = await _songService.ImportSongAsync(new ArchiveSongProvider(archive), (bpSong.SongName ?? bpSong.Hash) + ".zip");
                if (importResult.Type == FileImportResultType.Failed)
                {
                    Log.Error($"Failed to import song {bpSong.Hash}: {importResult.Error}");
                }
                else if (importResult.ImportedSong?.Hash != bpSong.Hash)
                {
                    Log.Warning($"Downloaded song {importResult.ImportedSong?.SongName} did NOT match expected hash of {bpSong.Hash}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to download/import {bpSong.Hash}");
            }

            progress?.ItemCompleted();
        });
    }
}
