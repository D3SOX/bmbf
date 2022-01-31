using System;
using System.Text.Json.Serialization;

namespace BMBF.Backend.Models;

/// <summary>
/// Represents a Beat Saber song
/// </summary>
public class Song : IEquatable<Song>
{
    public string Hash { get; }

    public string SongName { get; }

    public string SongSubName { get; }

    public string SongAuthorName { get; }

    public string LevelAuthorName { get; }

    public string Path { get; set; }

    public string CoverImageFileName { get; }

    public static Song CreateBlank(string? hash = null)
    {
        return new Song(hash ?? "", "", "", "", "", "", "");
    }

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

    public bool Equals(Song? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Hash == other.Hash &&
               SongName == other.SongName &&
               SongSubName == other.SongSubName &&
               SongAuthorName == other.SongAuthorName &&
               LevelAuthorName == other.LevelAuthorName &&
               Path == other.Path &&
               CoverImageFileName == other.CoverImageFileName;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;

        if (ReferenceEquals(this, obj)) return true;

        if (obj.GetType() != GetType()) return false;
        return Equals((Song)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Hash, SongName, SongSubName, SongAuthorName, LevelAuthorName, Path, CoverImageFileName);
    }

    public static bool operator ==(Song? left, Song? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Song? left, Song? right)
    {
        return !Equals(left, right);
    }
}