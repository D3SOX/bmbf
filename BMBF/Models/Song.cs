using Newtonsoft.Json;

namespace BMBF.Models
{
    /// <summary>
    /// Represents a Beat Saber song
    /// </summary>
    public class Song
    {
        public string Hash { get; }
        
        public string SongName { get; }
        
        public string SongSubName { get; }
        
        public string SongAuthorName { get; }
        
        public string LevelAuthorName { get; }
        
        public string Path { get; }
        
        public string CoverImageFileName { get; }
        
        [JsonConstructor]
        internal Song(string hash, string songName, string songSubName, string songAuthorName, string levelAuthorName, string path, string coverImageFileName)
        {
            Hash = hash;
            SongName = songName;
            SongSubName = songSubName;
            SongAuthorName = songAuthorName;
            LevelAuthorName = levelAuthorName;
            Path = path;
            CoverImageFileName = coverImageFileName;
        }
    }
}