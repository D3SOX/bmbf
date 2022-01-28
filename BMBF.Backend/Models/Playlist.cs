﻿using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
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
    public string PlaylistAuthor { get => _playlistAuthor; set { if (_playlistAuthor != value) { _playlistAuthor = value; NotifyInfoUpdated(); } } }
    private string _playlistAuthor;
        
    [JsonPropertyName("playlistDescription")]
    public string PlaylistDescription { get => _playlistDescription; set { if (_playlistDescription != value) { _playlistDescription = value; NotifyInfoUpdated(); } } }
    private string _playlistDescription;

    [JsonIgnore] public string Id { get; set; } = null!;

    [JsonPropertyName("image")]
    public string? ImageString {
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
            if (_image != value)
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
        set { 
            if (_songs != value)
            {
                _songs = value;
                NotifyUpdated(false, true, false);
            }
        }
    }
    private ImmutableList<BPSong> _songs;


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
    public string? LoadedFrom { get; set; }

    public event PlaylistUpdatedEventHandler? Updated;

    private void NotifyInfoUpdated() => NotifyUpdated(true, false, false);

    private void NotifyUpdated(bool infoUpdated, bool songsUpdated, bool coverUpdated)
    {
        Updated?.Invoke(this, infoUpdated, songsUpdated, coverUpdated);
        IsPendingSave = true;
    }

    [JsonConstructor]
    public Playlist(string playlistTitle, string playlistAuthor, string playlistDescription, ImmutableList<BPSong> songs, string? imageString)
    {
        _playlistTitle = playlistTitle;
        _playlistAuthor = playlistAuthor;
        _playlistDescription = playlistDescription;
        ImageString = imageString;
        _songs = songs;
    }

    /// <summary>
    /// More efficiently sets the info of the playlist by verifying that <see cref="Updated"/> is only invoked once.
    /// </summary>
    /// <param name="playlistInfo"></param>
    public void SetPlaylistInfo(PlaylistInfo playlistInfo)
    {
        // Update the properties first
        _playlistTitle = playlistInfo.PlaylistTitle;
        _playlistDescription = playlistInfo.PlaylistDescription;
        _playlistAuthor = playlistInfo.PlaylistAuthor;
        // ID is deliberately not reflected here - playlist ID should only be set once after playlist creation

        NotifyInfoUpdated();
    }
}