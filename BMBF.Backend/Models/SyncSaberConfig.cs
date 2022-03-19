using System.Collections.Generic;

namespace BMBF.Backend.Models;

public class SyncSaberConfig
{
    /// <summary>
    /// Settings for each map feed
    /// </summary>
    public Dictionary<FeedType, FeedSettings> Feeds { get; set; } = new();
    
    /// <summary>
    /// Username for syncing mappers & bookmarks from BeastSaber.
    /// These feeds will be ignored if this is null!
    /// </summary>
    public string? BeastSaberUsername { get; set; }
}
