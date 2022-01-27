using System;
using System.Text.Json.Serialization;

namespace BMBF.Backend.Models.BeatSaver;

/// <summary>
/// Structure of one version of a BeatSaver map.
/// This structure is incomplete
/// </summary>
public class MapVersion
{
    public MapVersion(string hash, Uri downloadUrl, DateTime createdAt = default)
    {
        Hash = hash;
        DownloadUrl = downloadUrl;
        CreatedAt = createdAt;
    }
    
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
    
    [JsonPropertyName("downloadURL")]
    public Uri DownloadUrl { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}