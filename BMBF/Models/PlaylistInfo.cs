
using Newtonsoft.Json;

namespace BMBF.Models
{
    /// <summary>
    /// Minimal format for initially sending playlists to the frontend
    /// </summary>
    public class PlaylistInfo
    { 
        public string? PlaylistId { get; set; }
        public string PlaylistTitle { get; set; }
        public string PlaylistAuthor { get; set; }
        public string PlaylistDescription { get; set; }
        
        public PlaylistInfo(Playlist playlist)
        {
            PlaylistTitle = playlist.PlaylistTitle;
            PlaylistAuthor = playlist.PlaylistAuthor;
            PlaylistDescription = playlist.PlaylistDescription;
        }
    }
}