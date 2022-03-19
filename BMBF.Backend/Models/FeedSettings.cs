namespace BMBF.Backend.Models;

public class FeedSettings
{
    /// <summary>
    /// The number of songs to add to the playlist
    /// </summary>
    public int SongsToSync { get; set; }
    
    /// <summary>
    /// Whether or not the feed will be synced when "Sync" is pressed
    /// </summary>
    public bool Enabled { get; set; }
}
