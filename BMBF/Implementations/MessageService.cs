using System;
using BMBF.Models;
using BMBF.Models.Messages;
using BMBF.Models.Setup;
using BMBF.Services;

namespace BMBF.Implementations
{
    public class MessageService : IMessageService
    {
        public event MessageEventHandler? MessageSend;

        private void Send(IMessage message) => MessageSend?.Invoke(message);

        public MessageService(
            ISetupService setupService,
            ISongService songService,
            IPlaylistService playlistService,
            IBeatSaberService beatSaberService)
        {
            // Register events to send our messages
            setupService.StatusChanged += OnSetupStatusUpdate;
            setupService.SetupComplete += OnSetupFinished;
            
            songService.SongAdded += OnSongAdded;
            songService.SongRemoved += OnSongRemoved;

            playlistService.PlaylistAdded += OnPlaylistAdded;
            playlistService.PlaylistDeleted += OnPlaylistRemoved;
            
            beatSaberService.AppChanged += OnAppChanged;
        }

        
        private void OnSongAdded(object? sender, Song song) => Send(new SongAdded(song));

        private void OnSongRemoved(object? sender, Song song) => Send(new SongRemoved(song.Hash));

        private void OnSetupStatusUpdate(object? sender, SetupStatus newStatus) => Send(new SetupStatusUpdate(newStatus));

        private void OnSetupFinished(object? sender, EventArgs args) => Send(new SetupFinished());

        private void OnPlaylistAdded(object? sender, Playlist playlist)
        {
            Send(new PlaylistAdded(new PlaylistInfo(playlist)));
            playlist.Updated += OnPlaylistUpdated;
        }

        private void OnPlaylistRemoved(object? sender, Playlist playlist)
        {
            playlist.Updated -= OnPlaylistUpdated;
            Send(new PlaylistRemoved(playlist.Id));
        }

        private void OnPlaylistUpdated(Playlist playlist, bool infoUpdated, bool songsUpdated, bool coverUpdated)
        {
            Send(new PlaylistUpdated
            {
                PlaylistInfo = infoUpdated ? new PlaylistInfo(playlist) : null,
                Songs = songsUpdated ? playlist.Songs : null,
                CoverUpdated = coverUpdated
            });
        }

        private void OnAppChanged(object? sender, InstallationInfo? newInstallationInfo) =>
            Send(new InstallationUpdated(newInstallationInfo));
    }
}