using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Models;
using Moq;
using Xunit;

namespace BMBF.Backend.Tests;

public class PlaylistServiceTests : IDisposable
{
    public PlaylistServiceTests()
    {
        _playlistService = CreatePlaylistService();
    }

    private readonly PlaylistService _playlistService;
    private readonly IFileSystem _fileSystem = new MockFileSystem();
    private readonly BMBFSettings _settings = new()
    {
        PlaylistsPath = "/Playlists",
        UpdateCachesAutomatically = false
    };
    
    private PlaylistService CreatePlaylistService() => new(_settings, _fileSystem, Mock.Of<IFileSystemWatcher>());

    [Fact]
    public async Task PlaylistsShouldBeEmptyWhenNoPlaylists()
    {
        var playlists = await _playlistService.GetPlaylistsAsync();
        Assert.Empty(playlists);
    }

    [Fact]
    public async Task ShouldAddPlaylist()
    {
        var playlists = await _playlistService.GetPlaylistsAsync();

        var examplePlaylist = Util.ExamplePlaylist;

        Playlist? addedPlaylist = null;
        _playlistService.PlaylistAdded += (_, playlist) => addedPlaylist = playlist;
        var playlistId = await _playlistService.AddPlaylistAsync(examplePlaylist);
        
        Assert.Equal(examplePlaylist, addedPlaylist);
        Assert.Equal(examplePlaylist, playlists[playlistId]);
    }

    [Fact]
    public async Task ShouldDeletePlaylistWhenDeleteInvoked()
    {
        var playlist = Util.ExamplePlaylist;
        
        var playlistId = await _playlistService.AddPlaylistAsync(playlist);
        await _playlistService.SavePlaylistsAsync();

        Playlist? deletedPlaylist = null;
        _playlistService.PlaylistDeleted += (_, p) => deletedPlaylist = p;
        await _playlistService.DeletePlaylistAsync(playlistId);
        
        Assert.Equal(deletedPlaylist, playlist);
        Assert.Empty(await _playlistService.GetPlaylistsAsync());
        Assert.False(_fileSystem.File.Exists(playlist.LoadedFrom));
    }

    [Fact]
    public async Task ShouldSavePlaylistToDisk()
    {
        var playlistId = await _playlistService.AddPlaylistAsync(Util.ExamplePlaylist);
        _playlistService.Dispose();

        using var newPlaylistService = CreatePlaylistService();
        var newPlaylists = await newPlaylistService.GetPlaylistsAsync();
        
        Assert.True(newPlaylists.ContainsKey(playlistId));
    }

    [Fact]
    public async Task ShouldLoadNewPlaylist()
    {
        var playlists = await _playlistService.GetPlaylistsAsync();
        await using (var playlistFileStream = _fileSystem.File.OpenWrite(Path.Combine(_settings.PlaylistsPath, "example.bplist")))
        {
            await JsonSerializer.SerializeAsync(playlistFileStream, Util.ExamplePlaylist);
        }

        await _playlistService.UpdatePlaylistCacheAsync();
        Assert.Single(playlists);
    }

    [Fact]
    public async Task ShouldRemoveDeletedPlaylist()
    {
        var playlist = Util.ExamplePlaylist;

        await _playlistService.AddPlaylistAsync(playlist);
        await _playlistService.SavePlaylistsAsync();
        Playlist? deleted = null;
        _playlistService.PlaylistDeleted += (_, p) => deleted = p;

        _fileSystem.File.Delete(playlist.LoadedFrom);
        await _playlistService.UpdatePlaylistCacheAsync();

        // As an unmodified playlist was deleted on disk, updating the cache should have triggered its removal
        Assert.Equal(playlist, deleted);
        Assert.Empty(await _playlistService.GetPlaylistsAsync());
    }

    [Fact]
    public async Task ShouldNotRemoveModifiedPlaylist()
    {
        var playlist = Util.ExamplePlaylist;

        await _playlistService.AddPlaylistAsync(playlist);
        await _playlistService.SavePlaylistsAsync();
        
        // As the playlist was modified, changes made in BMBF should be prioritised, so it will not be deleted
        playlist.PlaylistTitle = "New title";
        _fileSystem.File.Delete(playlist.LoadedFrom);
        await _playlistService.UpdatePlaylistCacheAsync();
        
        Assert.Single(await _playlistService.GetPlaylistsAsync());
    }

    [Fact]
    public async Task ShouldPrioritizeBMBFChanges()
    {
        var playlist = Util.ExamplePlaylist;
        await _playlistService.AddPlaylistAsync(playlist);
        await _playlistService.SavePlaylistsAsync();
        playlist.PlaylistTitle = "Modified title";

        var modifiedPlaylist = Util.ExamplePlaylist;
        modifiedPlaylist.PlaylistTitle = "Reloaded title";
        await using (var playlistFile = _fileSystem.File.OpenWrite(playlist.LoadedFrom))
        {
            await JsonSerializer.SerializeAsync(playlistFile, modifiedPlaylist);
        }
        await _playlistService.UpdatePlaylistCacheAsync();
        
        // As the playlist was modified in BMBF, the BMBF modifications should be prioritised over those made on disk
        Assert.Equal("Modified title", playlist.PlaylistTitle);
    }

    [Fact]
    public async Task ShouldReloadPlaylist()
    {
        var playlist = Util.ExamplePlaylist;
        await _playlistService.AddPlaylistAsync(playlist);
        await _playlistService.SavePlaylistsAsync();

        var modifiedPlaylist = Util.ExamplePlaylist;
        modifiedPlaylist.PlaylistTitle = "Reloaded title";
        _fileSystem.File.Delete(playlist.LoadedFrom);
        await using (var playlistFile = _fileSystem.File.OpenWrite(playlist.LoadedFrom))
        {
            await JsonSerializer.SerializeAsync(playlistFile, modifiedPlaylist);
        }
        await _playlistService.UpdatePlaylistCacheAsync();
        
        // As the playlist was NOT modified, it should have been reloaded from disk
        Assert.Equal("Reloaded title", playlist.PlaylistTitle);
    }

    public void Dispose()
    {
        _playlistService.Dispose();
    }
}