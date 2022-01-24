using System.Text.Json.Serialization;

namespace BMBF.Backend.Models;

/// <summary>
/// Minimal format for initially sending playlists to the frontend
/// </summary>
public class PlaylistInfo
{
    public string Id { get; set; }
    public string PlaylistTitle { get; set; }
    public string PlaylistAuthor { get; set; }
    public string PlaylistDescription { get; set; }

    public PlaylistInfo(Playlist playlist) : this(playlist.Id, playlist.PlaylistTitle, playlist.PlaylistAuthor,
        playlist.PlaylistDescription)
    {
    }

    [JsonConstructor]
    public PlaylistInfo(string id, string playlistTitle, string playlistAuthor, string playlistDescription)
    {
        Id = id;
        PlaylistTitle = playlistTitle;
        PlaylistAuthor = playlistTitle;
        PlaylistDescription = playlistTitle;
    }
}