#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BMBF.Util.Song;
using Newtonsoft.Json;
using Serilog;

namespace BMBF.Util
{
    public static class SongUtil
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();

        private static async Task<string?> TryGetSongHashAsync(string path, string infoDatPath, BeatmapInfoDat infoDat)
        {
            using var hash = SHA1.Create();
            await using var dataStream = new MemoryStream();

            using var infoDatStream = File.OpenRead(infoDatPath);
            await infoDatStream.CopyToAsync(dataStream);

            foreach (var difficultySet in infoDat.DifficultyBeatmapSets)
            {
                foreach (var difficulty in difficultySet.DifficultyBeatmaps)
                {
                    string beatmapFilePath = Path.Combine(path, difficulty.BeatmapFilename);
                    if (!File.Exists(beatmapFilePath))
                    {
                        Log.Warning($"Song missing beatmap difficulty file named {difficulty.BeatmapFilename}");
                        return null;
                    }

                    await using var difficultyStream = File.OpenRead(beatmapFilePath);
                    await difficultyStream.CopyToAsync(dataStream);
                }
            }

            return BitConverter.ToString(hash.ComputeHash(dataStream.ToArray())).Replace("-", "").ToUpper();
        }
        
        /// <summary>
        /// Attempts to load a song from the given path
        /// </summary>
        /// <param name="path">The path of the song</param>
        /// <returns>The loaded Song model, or null if a song could not be loaded from the path</returns>
        /// <exception cref="DirectoryNotFoundException">If the song directory does not exist</exception>
        public static async Task<Models.Song?> TryLoadSongInfoAsync(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("Attempted to load song from folder which does not exist");
            }

            string infoDatPath = Path.Combine(path, "info.dat");
            if (!File.Exists(infoDatPath))
            {
                infoDatPath = "Info.dat";
            }

            if (!File.Exists(infoDatPath))
            {
                Log.Warning($"Could not load song from {path} - missing info.dat/Info.dat");
                return null;
            }

            BeatmapInfoDat infoDat;
            using (var reader = new StreamReader(infoDatPath))
            {
                using var jsonReader = new JsonTextReader(reader);
                infoDat = JsonSerializer.Deserialize<BeatmapInfoDat>(jsonReader);
            }

            if (!File.Exists(Path.Combine(path, infoDat.CoverImageFilename)))
            {
                Log.Warning($"Song missing cover {infoDat.CoverImageFilename}");
            }

            string? hash = await TryGetSongHashAsync(path, infoDatPath, infoDat);
            if (hash == null)
            {
                return null;
            }
            return new Models.Song(hash, infoDat.SongName, infoDat.SongSubName, infoDat.SongAuthorName, infoDat.LevelAuthorName, path, infoDat.CoverImageFilename);
        }
    }
}