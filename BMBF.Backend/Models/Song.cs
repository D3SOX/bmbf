using System.Text.Json.Serialization;

namespace BMBF.Backend.Models;

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
        
    public string Path { get; set; }
        
    public string CoverImageFileName { get; }
        
    [JsonConstructor]
    public Song(string hash, string songName, string songSubName, string songAuthorName, string levelAuthorName, string path, string coverImageFileName)
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