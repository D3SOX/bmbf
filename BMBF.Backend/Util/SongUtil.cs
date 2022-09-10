using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using BMBF.Backend.Util.Song;
using Serilog;

namespace BMBF.Backend.Util;

public static class SongUtil
{
    private static async Task HashStreamAsync(HashAlgorithm hash, Stream stream, byte[] buffer)
    {
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            hash.TransformBlock(buffer, 0, read, buffer, 0);
        }
    }

    private static async Task<string?> TryGetSongHashAsync(ISongProvider provider, Stream infoDatStream, BeatmapInfoDat infoDat)
    {
        using var hash = SHA1.Create();

        var buffer = new byte[4096];
        await HashStreamAsync(hash, infoDatStream, buffer);
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
                await HashStreamAsync(hash, difficultyStream, buffer);
            }
        }
        hash.TransformFinalBlock(buffer, 0, 0);

        return BitConverter.ToString(hash.Hash!).Replace("-", "").ToUpper();
    }

    /// <summary>
    /// Attempts to load a song from the given path
    /// </summary>
    /// <param name="provider">Provider to load the song info from</param>
    /// <param name="name">Name to use for logging purposes</param>
    /// <returns>The loaded Song model, or null if a song could not be loaded from the path</returns>
    public static async Task<Backend.Models.Song?> TryLoadSongInfoAsync(ISongProvider provider, string? name = null)
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
        await using (var infoDatStream = provider.Open(infoDatPath))
        {
            infoDat = await JsonSerializer.DeserializeAsync<BeatmapInfoDat>(infoDatStream);
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
