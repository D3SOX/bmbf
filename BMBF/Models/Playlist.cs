using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BMBF.Util.BPList;
using Newtonsoft.Json;

namespace BMBF.Models
{
    /// <summary>
    /// Represents a playlist in the BMBF cache
    /// This format is compatible with BPList to make loading/saving easier
    /// </summary>
    public class Playlist : INotifyPropertyChanged
    {
        public string PlaylistTitle { get => _playlistTitle; set { if (_playlistTitle != value) { _playlistTitle = value; NotifyPropertyChanged(); } } }
        private string _playlistTitle;
        
        public string PlaylistAuthor { get => _playlistAuthor; set { if (_playlistAuthor != value) { _playlistAuthor = value; NotifyPropertyChanged(); } } }
        private string _playlistAuthor;
        
        public string PlaylistDescription { get => _playlistDescription; set { if (_playlistDescription != value) { _playlistDescription = value; NotifyPropertyChanged(); } } }
        private string _playlistDescription;
        
        
        [JsonProperty("image")]
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
        public byte[]? Image { get; set; }
        
        [JsonIgnore]
        public bool IsPendingSave { get; set; }

        /// <summary>
        /// The songs within the playlist
        /// </summary>
        public ObservableCollection<BPSong> Songs
        {
            get => _songs; 
            set { 
                if (_songs != value)
                {
                    _songs.CollectionChanged -= SongsCollectionChanged;
                    _songs = value;
                    _songs.CollectionChanged += SongsCollectionChanged;
                    NotifyPropertyChanged();
                }
            }
        }

        private ObservableCollection<BPSong> _songs;


        // The below intentionally do not notify changes
        
        /// <summary>
        /// The time that the playlist was loaded from the playlists folder
        /// Used to avoid reloading playlists unless necessary
        /// </summary>
        [JsonIgnore]
        public DateTime LastLoadTime { get; set; }

        [JsonIgnore] public string PlaylistId { get; set; } = null!;

        public event PropertyChangedEventHandler? PropertyChanged;
        
        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            IsPendingSave = true;
        }

        private void SongsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            NotifyPropertyChanged(nameof(Songs));
        }

        [JsonConstructor]
        public Playlist(string playlistTitle, string playlistAuthor, string playlistDescription, ObservableCollection<BPSong> songs, string? image)
        {
            _playlistTitle = playlistTitle;
            _playlistAuthor = playlistAuthor;
            _playlistDescription = playlistDescription;
            ImageString = image;
            _songs = songs;
            _songs.CollectionChanged += SongsCollectionChanged;
        }
    }
}