using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models.BPList;

namespace BMBF.Backend.Models;

/// <summary>
/// Represents a playlist in the BMBF cache
/// This format is compatible with BPList to make loading/saving easier
/// </summary>
public class Playlist
{
    public const string LegalIdCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";

    [JsonPropertyName("playlistTitle")]
    public string PlaylistTitle { get => _playlistTitle; set { if (_playlistTitle != value) { _playlistTitle = value; NotifyInfoUpdated(); } } }
    private string _playlistTitle;

    [JsonPropertyName("playlistAuthor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PlaylistAuthor { get => _playlistAuthor; set { if (_playlistAuthor != value) { _playlistAuthor = value; NotifyInfoUpdated(); } } }
    private string? _playlistAuthor;

    [JsonPropertyName("playlistDescription")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PlaylistDescription { get => _playlistDescription; set { if (_playlistDescription != value) { _playlistDescription = value; NotifyInfoUpdated(); } } }
    private string? _playlistDescription;

    [JsonIgnore] public string Id { get; set; } = null!;

    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageString
    {
        get => Image == null ? null : "data:image/png;base64," + Convert.ToBase64String(Image);
        set
        {
            if (value == null)
            {
                Image = null;
            }
            else
            {
                var idx = value.IndexOf("base64,", StringComparison.Ordinal);
                Image = Convert.FromBase64String(idx == -1 ? value : value.Substring(idx + 7));
            }
        }
    }

    [JsonIgnore]
    public byte[]? Image
    {
        get => _image;
        set
        {
            if (!_image.NullableSequenceEquals(value))
            {
                _image = value;
                NotifyUpdated(false, false, true);
            }
        }
    }
    private byte[]? _image;

    [JsonIgnore]
    public bool IsPendingSave { get; set; }

    /// <summary>
    /// The songs within the playlist
    /// </summary>
    [JsonPropertyName("songs")]
    public ImmutableList<BPSong> Songs
    {
        get => _songs;
        set
        {
            if (!_songs.SequenceEqual(value))
            {
                _songs = value;
                NotifyUpdated(false, true, false);
            }
        }
    }
    private ImmutableList<BPSong> _songs;
    
    /// <summary>
    /// Additional properties that may be present in the playlist which BMBF does not process.
    /// Examples include the <code>customData</code> property, or <code>syncURL</code>.
    ///
    /// In general, the playlist format used by various Beat Saber related tools is pretty inconsistent so instead
    /// of manually specifying a bunch of properties we'll just use <see cref="JsonExtensionDataAttribute"/>.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
    
    /// <summary>
    /// If non-null, this indicates the SyncSaber feed that this playlist was synced from
    /// </summary>
    public FeedType? SyncSaberFeed { get; }

    // The below intentionally do not notify changes

    /// <summary>
    /// The time that the playlist was loaded from the playlists folder
    /// Used to avoid reloading playlists unless necessary
    /// </summary>
    [JsonIgnore]
    public DateTime LastLoadTime { get; set; }

    /// <summary>
    /// Path that the playlist was loaded from
    /// </summary>
    [JsonIgnore]
    public string? LoadedFrom { get; set; }

    public event PlaylistUpdatedEventHandler? Updated;

    private void NotifyInfoUpdated() => NotifyUpdated(true, false, false);

    private void NotifyUpdated(bool infoUpdated, bool songsUpdated, bool coverUpdated)
    {
        Updated?.Invoke(this, infoUpdated, songsUpdated, coverUpdated);
        IsPendingSave = true;
    }

    [JsonConstructor]
    public Playlist(string playlistTitle,
        string? playlistAuthor,
        string? playlistDescription,
        ImmutableList<BPSong> songs,
        string? imageString = null,
        FeedType? syncSaberFeed = null)
    {
        _playlistTitle = playlistTitle;
        _playlistAuthor = playlistAuthor;
        _playlistDescription = playlistDescription;
        ImageString = imageString;
        _songs = songs;
        SyncSaberFeed = syncSaberFeed;
    }

    /// <summary>
    /// More efficiently sets the info of the playlist by verifying that <see cref="Updated"/> is only invoked once.
    /// </summary>
    /// <param name="playlistInfo"></param>
    public void SetPlaylistInfo(PlaylistInfo playlistInfo)
    {
        // Update the properties first
        if (playlistInfo.PlaylistTitle != _playlistTitle || playlistInfo.PlaylistAuthor != _playlistAuthor ||
            playlistInfo.PlaylistDescription != _playlistDescription)
        {
            _playlistTitle = playlistInfo.PlaylistTitle;
            _playlistDescription = playlistInfo.PlaylistDescription;
            _playlistAuthor = playlistInfo.PlaylistAuthor;
            NotifyInfoUpdated();
            // ID is deliberately not reflected here - playlist ID should only be set once after playlist creation
        }
    }
}
