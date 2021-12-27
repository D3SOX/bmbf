#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace BMBF.Models
{
    /// <summary>
    /// Represents a playlist in the BMBF cache
    /// </summary>
    public class Playlist : INotifyPropertyChanged
    {
        public string PlaylistId { get => _playlistId; set { if (_playlistId != value) { _playlistId = value; NotifyPropertyChanged(); } } }
        private string _playlistId;
        
        public string PlaylistName { get => _playlistName; set { if (_playlistName != value) { _playlistName = value; NotifyPropertyChanged(); } } }
        private string _playlistName;
        
        [JsonIgnore]
        public ObservableCollection<Song> Songs { get => _songs; set { if (Songs != value) { _songs = value; NotifyPropertyChanged(); } } }
        private ObservableCollection<Song> _songs;

        [JsonProperty("songs")]
        public IEnumerable<string> SongHashes => Songs.Select(s => s.Hash);
        
        public string Path { get; }

        // The below intentionally do not notify changes
        
        /// <summary>
        /// The time that the playlist was loaded from the playlists folder
        /// Used to avoid reloading playlists unless necessary
        /// </summary>
        public DateTime LastLoadTime { get; set; }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Playlist(string playlistId, string playlistName, string path, ObservableCollection<Song> songs, DateTime lastLoadTime)
        {
            _playlistId = playlistId;
            _playlistName = playlistName;
            _songs = songs;
            Path = path;
            LastLoadTime = lastLoadTime;
        }
    }
}