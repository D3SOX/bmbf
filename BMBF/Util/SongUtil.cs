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

        private static async Task<string> GetSongHashAsync(string path, string infoDatPath, BeatmapInfoDat infoDat)
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
                        throw new FileNotFoundException(
                            $"Song missing beatmap difficulty file named {difficulty.BeatmapFilename}");
                    }

                    await using var difficultyStream = File.OpenRead(beatmapFilePath);
                    await difficultyStream.CopyToAsync(dataStream);
                }
            }

            return BitConverter.ToString(hash.ComputeHash(dataStream.ToArray())).Replace("-", "").ToUpper();
        }
        
        /// <summary>
        /// Loads a song from the given path
        /// </summary>
        /// <param name="path">The path of the song</param>
        /// <returns>The loaded Song model</returns>
        /// <exception cref="DirectoryNotFoundException">If the song directory does not exist</exception>
        /// <exception cref="FileNotFoundException">If no info.dat or Info.dat file is present in the song directory, the cover is missing, or any of the difficulty files of the song are missing</exception>
        public static async Task<Models.Song> LoadSongInfoAsync(string path)
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
                throw new FileNotFoundException("No info.dat or Info.dat file found in song folder");
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

            string hash = await GetSongHashAsync(path, infoDatPath, infoDat);
            return new Models.Song(hash, infoDat.SongName, infoDat.SongSubName, infoDat.SongAuthorName, infoDat.LevelAuthorName, path, infoDat.CoverImageFilename);
        }
    }
}