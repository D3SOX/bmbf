using System;
using System.Threading;
using BMBF.Backend.Models;
using BMBF.Backend.Models.Messages;
using BMBF.Backend.Models.Setup;
using BMBF.Backend.Services;
using BMBF.ModManagement;
using Serilog;

namespace BMBF.Backend.Implementations;

public class MessageService : IMessageService
{
    public event MessageEventHandler? MessageSend;

    private void Send(IMessage message) => MessageSend?.Invoke(message);
    
    public MessageService(
        ISetupService setupService,
        ISongService songService,
        IPlaylistService playlistService,
        IBeatSaberService beatSaberService,
        IModService modService,
        IProgressService progressService)
    {
        MessageSend += OnMessageSend;
        
        // Register events to send our messages
        setupService.StatusChanged += OnSetupStatusUpdate;
        setupService.SetupComplete += OnSetupFinished;

        songService.SongAdded += OnSongAdded;
        songService.SongRemoved += OnSongRemoved;

        playlistService.PlaylistAdded += OnPlaylistAdded;
        playlistService.PlaylistDeleted += OnPlaylistRemoved;

        beatSaberService.AppChanged += OnAppChanged;

        modService.ModAdded += OnModAdded;
        modService.ModRemoved += OnModRemoved;
        modService.ModStatusChanged += OnModStatusChanged;

        progressService.Added += OnProgressAdded;
        progressService.Updated += OnProgressUpdated;
        progressService.Removed += OnProgressRemoved;
    }

    private void OnMessageSend(IMessage message)
    {
        Log.Verbose($"Message sent: {message.Type}");
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

    private void OnModAdded(object? sender, IMod mod) => Send(new ModAdded(mod));

    private void OnModRemoved(object? sender, string modId) => Send(new ModRemoved(modId));

    private void OnModStatusChanged(object? sender, IMod mod) => Send(new ModStatusChanged(mod.Id, mod.Installed));

    private void OnProgressAdded(object? sender, IProgress progress) => Send(new ProgressAdded(progress));

    private void OnProgressUpdated(object? sender, IProgress progress) => Send(new ProgressUpdated(progress));
    private void OnProgressRemoved(object? sender, IProgress progress) => Send(new ProgressRemoved(progress));
}
