﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BMBF.Backend.Util.Song;
using Newtonsoft.Json;
using Serilog;

namespace BMBF.Backend.Util;

public static class SongUtil
{
    private static readonly JsonSerializer JsonSerializer = new();

    private static async Task<string?> TryGetSongHashAsync(IFolderProvider provider, Stream infoDatStream, BeatmapInfoDat infoDat)
    {
        using var hash = SHA1.Create();
        await using var dataStream = new MemoryStream();

        await infoDatStream.CopyToAsync(dataStream);

        foreach (var difficultySet in infoDat.DifficultyBeatmapSets)
        {
            foreach (var difficulty in difficultySet.DifficultyBeatmaps)
            {
                if (!provider.Exists(difficulty.BeatmapFilename))
                {
                    Log.Warning($"Song missing beatmap difficulty file named {difficulty.BeatmapFilename}");
                    return null;
                }

                await using var difficultyStream = provider.Open(difficulty.BeatmapFilename);
                await difficultyStream.CopyToAsync(dataStream);
            }
        }

        return BitConverter.ToString(hash.ComputeHash(dataStream.ToArray())).Replace("-", "").ToUpper();
    }
        
    /// <summary>
    /// Attempts to load a song from the given path
    /// </summary>
    /// <param name="provider">Provider to load the song info from</param>
    /// <param name="name">Name to use for logging purposes</param>
    /// <returns>The loaded Song model, or null if a song could not be loaded from the path</returns>
    public static async Task<Backend.Models.Song?> TryLoadSongInfoAsync(IFolderProvider provider, string? name = null)
    {
        name ??= "<unknown>";

        string infoDatPath = "info.dat";
        if (!provider.Exists(infoDatPath))
        {
            infoDatPath = "Info.dat";
        }

        if (!provider.Exists(infoDatPath))
        {
            Log.Warning($"Could not load song from {name} - missing info.dat/Info.dat");
            return null;
        }

        BeatmapInfoDat? infoDat;
        await using(var infoDatStream = provider.Open(infoDatPath))
        using(var infoDatReader = new StreamReader(infoDatStream))
        using (var jsonReader = new JsonTextReader(infoDatReader))
        {
            infoDat = JsonSerializer.Deserialize<BeatmapInfoDat>(jsonReader);
            if (infoDat == null)
            {
                Log.Warning($"Info.dat for song {name} was null");
                return null;
            }
        }

        if (!provider.Exists(infoDat.CoverImageFilename))
        {
            Log.Warning($"Song missing cover {infoDat.CoverImageFilename}");
        }

        // Re-open info.dat from the beginning to calculate the song hash
        // Note that we cannot seek as the result of provider.Open isn't necessarily seekable
        await using (var infoDatStream = provider.Open(infoDatPath))
        {
            string? hash = await TryGetSongHashAsync(provider, infoDatStream, infoDat);
            if (hash == null)
            {
                return null;
            }

            return new Backend.Models.Song(hash, infoDat.SongName, infoDat.SongSubName, infoDat.SongAuthorName,
                infoDat.LevelAuthorName, null!, infoDat.CoverImageFilename);
        }
    }
}