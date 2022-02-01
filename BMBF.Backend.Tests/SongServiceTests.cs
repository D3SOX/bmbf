using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Models;
using Moq;
using Xunit;

namespace BMBF.Backend.Tests;

public class SongServiceTests : IDisposable
{
    public SongServiceTests()
    {
        _songService = CreateSongService();
    }

    private SongService CreateSongService() => new(_settings, _fileSystem, Mock.Of<IFileSystemWatcher>());

    private readonly SongService _songService;
    private readonly MockFileSystem _fileSystem = new();
    private readonly BMBFSettings _settings = new()
    {
        SongsCacheName = "songCache.json",
        RootDataPath = "/BMBFData",
        SongsPath = "/Songs"
    };

    [Fact]
    public async Task SongsShouldBeEmptyWhenNoSongs()
    {
        var songs = await _songService.GetSongsAsync();
        Assert.Empty(songs);
    }

    [Fact]
    public async Task ShouldAddSongWhenImporting()
    {
        var songs = await _songService.GetSongsAsync();
        Song? addedSong = null;
        _songService.SongAdded += (_, song) => addedSong = song;

        await _songService.ImportSongAsync(Util.ExampleSongProvider, "example.zip");

        Assert.Equal(songs.Values.Single(), addedSong);
    }

    [Fact]
    public async Task ShouldSaveSongToDisk()
    {
        await _songService.ImportSongAsync(Util.ExampleSongProvider, "example.zip");
        var songs = await _songService.GetSongsAsync();

        _songService.Dispose();
        using var newSongService = CreateSongService();
        var newSongs = await _songService.GetSongsAsync();

        Assert.Equal(songs.Single(), newSongs.Single());
    }

    [Fact]
    public async Task ShouldLoadNewSongFromDisk()
    {
        // Load existing songs first
        var songs = await _songService.GetSongsAsync();

        // Now copy a new song into the songs directory and update the song cache
        await Util.ExampleSongProvider.CopyToAsync(Path.Combine(_settings.SongsPath, "Example_Song"), _fileSystem);
        Song? addedSong = null;
        _songService.SongAdded += (_, song) => addedSong = song;
        await _songService.UpdateSongCacheAsync();

        Assert.Single(songs); // There should now be 1 song
        Assert.NotNull(addedSong); // SongAdded should have been invoked
    }

    [Fact]
    public async Task ShouldDeleteSongRemovedOnDisk()
    {
        // Import a song
        await _songService.ImportSongAsync(Util.ExampleSongProvider, "song.zip");
        var songs = await _songService.GetSongsAsync();
        var song = songs.Values.Single();

        // Remove the song on disk and update the song cache
        _fileSystem.Directory.Delete(song.Path, true);
        Song? removedSong = null;
        _songService.SongRemoved += (_, s) => removedSong = s;
        await _songService.UpdateSongCacheAsync();

        Assert.Empty(songs); // The song should be removed
        Assert.Equal(song, removedSong);
    }

    [Fact]
    public async Task ShouldDeleteSongWhenDeleteInvoked()
    {
        await _songService.ImportSongAsync(Util.ExampleSongProvider, "song.zip");
        var songs = await _songService.GetSongsAsync();

        var song = songs.Values.Single();
        Song? removedSong = null;
        _songService.SongRemoved += (_, s) => removedSong = s;
        await _songService.DeleteSongAsync(song.Hash);

        Assert.Empty(songs);
        Assert.False(_fileSystem.Directory.Exists(song.Path));
        Assert.Equal(song, removedSong);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldDeleteDuplicateFolders(bool deleteDuplicates)
    {
        _settings.DeleteDuplicateSongs = deleteDuplicates;
        using var songService = CreateSongService();
        await songService.ImportSongAsync(Util.ExampleSongProvider, "song.zip");
        await Util.ExampleSongProvider.CopyToAsync(Path.Combine(_settings.SongsPath, "Example_Duplicate"), _fileSystem);

        await songService.UpdateSongCacheAsync();

        var songDirectoryCount = _fileSystem.Directory.EnumerateDirectories(_settings.SongsPath).Count();
        if (deleteDuplicates)
        {
            // Deleting duplicates is enabled, so this song should be deleted
            Assert.Equal(1, songDirectoryCount);
        }
        else
        {
            // Deleting duplicates is disabled, so this song should be left as is
            Assert.Equal(2, songDirectoryCount);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldDeleteInvalidFolders(bool deleteInvalids)
    {
        _settings.DeleteInvalidSongs = deleteInvalids;
        using var songService = CreateSongService();
        var invalidSongPath = Path.Combine(_settings.SongsPath, "Example_Invalid");
        _fileSystem.Directory.CreateDirectory(invalidSongPath);

        await songService.UpdateSongCacheAsync();

        // If we enable deleting invalid folders, then the folder should not exist, otherwise it should
        Assert.Equal(deleteInvalids, !_fileSystem.Directory.Exists(invalidSongPath));
    }

    public void Dispose()
    {
        _songService.Dispose();
    }
}