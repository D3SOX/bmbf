using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.Backend.Util.BPList;
using Newtonsoft.Json;
using Serilog;

namespace BMBF.Backend.Implementations;

public class FileImporter : IFileImporter
{
    private readonly ISongService _songService;
    private readonly IPlaylistService _playlistService;
    private readonly IBeatSaverService _beatSaverService;
    private readonly BMBFSettings _bmbfSettings;
    private readonly IExtensionsService _extensionsService;

    public FileImporter(ISongService songService,
        IPlaylistService playlistService,
        IBeatSaverService beatSaverService,
        BMBFSettings bmbfSettings,
        IExtensionsService extensionsService)
    {
        _songService = songService;
        _playlistService = playlistService;
        _beatSaverService = beatSaverService;
        _bmbfSettings = bmbfSettings;
        _extensionsService = extensionsService;
    }
        
    private async Task<string?> TryImportPlaylistAsync(Stream stream, string fileName)
    {
        Log.Information($"Importing {fileName} as playlist");
        Playlist playlist;
        try
        {
            playlist = stream.ReadAsJson<Playlist>();
        }
        catch (JsonException ex)
        {
            Log.Error($"Could not parse as playlist: {ex.Message}");
            return null;
        }
            
        // Install any missing songs in the BPList
        // TODO: Progress bar system
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
        }
        else
        {
            Log.Information($"Found {missingSongs.Count} songs that will need to be installed");
        }
            
        foreach (var bpSong in missingSongs)
        {
            Log.Information($"Downloading song {bpSong.SongName ?? bpSong.Hash}");
            try
            {
                await using var mapStream = bpSong.Key == null ? await _beatSaverService.DownloadSongByHash(bpSong.Hash) : await _beatSaverService.DownloadSongByKey(bpSong.Key);
                if (mapStream == null)
                {
                    Log.Warning($"No map with hash {bpSong.Hash} or with key {bpSong.Key} found on BeatSaver");
                    continue;
                }
                    
                using var archive = new ZipArchive(mapStream, ZipArchiveMode.Read);
                var importResult = await _songService.ImportSongAsync(archive, (bpSong.SongName ?? bpSong.Hash) + ".zip");
                if (importResult.Type == FileImportResultType.Failed)
                {
                    Log.Error($"Failed to import song {bpSong.Hash}: {importResult.Error}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to download/import {bpSong.Hash}");
            }
        }

        // TODO: Add playlist before all the songs are downloaded, or wait until all finished?
        // For now we are waiting until all of the songs are downloaded
        var playlistId = await _playlistService.AddPlaylistAsync(playlist);
        Log.Information($"Playlist file {fileName} imported as {playlistId}");
        return playlistId;
    }

    private async Task CopyFile(Stream stream, string fileName, string saveDirectory)
    {
        Directory.CreateDirectory(saveDirectory);
        var outputPath = Path.Combine(saveDirectory, fileName);
        if(File.Exists(outputPath)) File.Delete(outputPath);
                
        Log.Information($"Copied {fileName} to {saveDirectory}");
        await using var outputStream = File.OpenWrite(outputPath);
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
                return await _songService.ImportSongAsync(archive, fileName);
            }
            catch (InvalidDataException)
            {
                return FileImportResult.CreateError("The given song was not a valid ZIP archive");
            }
        }

        await _extensionsService.LoadExtensions();
            
        if (_extensionsService.ConfigExtensions.Contains(extension))
        {
            string modId = Path.GetFileNameWithoutExtension(fileName);
            bool modExistsWithId = false; // TODO: When mods are implemented, check if a mod actually exists with the ID

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
            
        if (_extensionsService.PlaylistExtensions.Contains(extension))
        {
            var playlistId = await TryImportPlaylistAsync(stream, fileName);
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
            
        // TODO: Attempt to import as a mod here

        if (_extensionsService.CopyExtensions.TryGetValue(extension, out var copyInfo))
        {
            await CopyFile(stream, fileName, copyInfo.Destination);
            Log.Information($"{fileName} copied via copy extension ({extension})");
            if (copyInfo.ModId != null)
            {
                Log.Information($"The mod registering this extension was {copyInfo.ModId}");
            }

            return new FileImportResult
            {
                Type = FileImportResultType.FileCopy,
                FileCopyInfo = copyInfo
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
}