using System.Collections.Immutable;
using System.Text.Json.Serialization;
using BMBF.Backend.Models.BPList;

namespace BMBF.Backend.Models.Messages;

public class PlaylistUpdated : IMessage
{
    public MessageType Type => MessageType.PlaylistUpdated;

    /// <summary>
    /// The updated songs of the playlist, null if the songs did not change
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableList<BPSong>? Songs { get; set; }

    /// <summary>
    /// The updated details of the playlist, null if they did not change
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PlaylistInfo? PlaylistInfo { get; set; }

    /// <summary>
    /// Whether or not the cover of this playlist has changed
    /// </summary>
    public bool CoverUpdated { get; set; }
}
