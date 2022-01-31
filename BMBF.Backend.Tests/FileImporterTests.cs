using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Models;
using BMBF.Backend.Models.BPList;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using BMBF.ModManagement;
using BMBF.Resources;
using Moq;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BMBF.Backend.Tests;

public class FileImporterTests
{
    private static readonly byte[] ExampleFileContent = Encoding.UTF8.GetBytes("Hello World!");
    public FileImporterTests()
    {
        _modServiceMock.Setup(m => m.GetModsAsync()).ReturnsAsync(new Dictionary<string, (IMod mod, string path)>());
        _songServiceMock.Setup(s => s.GetSongsAsync()).ReturnsAsync(new Dictionary<string, Song>());
        _assetServiceMock.Setup(m => m.GetExtensions())
        .ReturnsAsync(() => new FileExtensions(
            new Dictionary<string, string> { { "qsaber", "/Sabers" } },
            new List<string> { "json", "bplist" },
            new List<string> { "json", "yml" }
        ));
        _fileImporter = new FileImporter(
            _songServiceMock.Object,
            _playlistServiceMock.Object,
            _beatSaverServiceMock.Object,
            _settings,
            _modServiceMock.Object,
            _assetServiceMock.Object,
            _fileSystem
        );
    }

    private readonly FileImporter _fileImporter;
    private readonly Mock<ISongService> _songServiceMock = new();
    private readonly Mock<IBeatSaverService> _beatSaverServiceMock = new();
    private readonly Mock<IAssetService> _assetServiceMock = new();
    private readonly Mock<IModService> _modServiceMock = new();
    private readonly Mock<IPlaylistService> _playlistServiceMock = new();

    private readonly IFileSystem _fileSystem = new MockFileSystem();
    private readonly BMBFSettings _settings = new()
    {
        ConfigsPath = "/Configs"
    };

    /// <summary>
    /// Sets up <see cref="_modServiceMock"/> to return the given mods
    /// </summary>
    /// <param name="mods">Mods to mock as loaded</param>
    private void SetupMods(params IMod[] mods)
    {
        var modsDictionary = mods.ToDictionary(m => m.Id, m => (m, ""));
        _modServiceMock.Setup(m => m.GetModsAsync())
            .ReturnsAsync(modsDictionary);
    }

    /// <summary>
    /// Creates a stream containing the bytes in <see cref="ExampleFileContent"/>
    /// </summary>
    /// <returns>Stream containing the bytes in <see cref="ExampleFileContent"/>, seeked to position 0</returns>
    private Stream CreateExampleContentStream()
    {
        var stream = new MemoryStream();
        stream.Write(ExampleFileContent);
        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Serializes the given playlist to a stream.
    /// </summary>
    /// <param name="playlist">Playlist to serialize</param>
    /// <returns>Stream containing the JSON playlist</returns>
    private Stream CreatePlaylistStream(Playlist playlist)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, playlist,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        stream.Position = 0;

        return stream;
    }

    private Stream CreateEmptyZipStream()
    {
        var zipStream = new MemoryStream();
        new ZipArchive(zipStream, ZipArchiveMode.Update, true).Dispose();
        zipStream.Position = 0;
        return zipStream;
    }

    [Fact]
    public async Task ShouldImportZipAsSong()
    {
        var songStream = CreateEmptyZipStream();
        var expectedResult = new FileImportResult
        {
            ImportedSong = Song.CreateBlank(),
            Type = FileImportResultType.Song
        };

        _songServiceMock.Setup(s =>
            s.ImportSongAsync(It.IsNotNull<ISongProvider>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResult);

        var result = await _fileImporter.ImportAsync(songStream, "myFile.zip");

        // Make sure that the result returns matches that returned by the ISongService
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ShouldErrorWithInvalidArchive()
    {
        // Blank stream will not be a valid archive
        using var modStream = new MemoryStream();

        var result = await _fileImporter.TryImportAsync(modStream, "myFile.zip");

        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldErrorWhenCannotLoadExtensions()
    {
        _assetServiceMock.Setup(a => a.GetExtensions()).ThrowsAsync(new HttpRequestException());

        using var blankStream = new MemoryStream();
        // QSABER is arbitrary, this can be any file type that is NOT .ZIP
        var result = await _fileImporter.TryImportAsync(blankStream, "myFile.qsaber");

        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldNotImportUnknownConfig()
    {
        using var blankStream = new MemoryStream();
        // YAML is used instead of JSON here, since we don't want to trigger playlist importing
        var result = await _fileImporter.TryImportAsync(blankStream, "myUnknownConfig.yml");

        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldImportKnownConfig()
    {
        var modMock = new Mock<IMod>();
        modMock.SetupGet(m => m.Id)
            .Returns("example-mod");
        // Add an example mod which will allow a config with its ID to be imported
        SetupMods(modMock.Object);

        // Import a config with the mod ID as the filename, and read the content placed in the config path
        using var exampleContent = CreateExampleContentStream();
        await _fileImporter.TryImportAsync(exampleContent, $"{modMock.Object.Id}.json");
        byte[] content = _fileSystem.File.ReadAllBytes(Path.Combine(_settings.ConfigsPath, "example-mod.json"));

        Assert.Equal(ExampleFileContent, content);
    }

    [Fact]
    public async Task ShouldUtiliseModCopyExtension()
    {
        var modMock = new Mock<IMod>();
        modMock
            .SetupGet(m => m.Id)
            .Returns("example-mod");
        modMock.SetupGet(m => m.CopyExtensions)
            .Returns(new Dictionary<string, string> { { "png", "/ExampleModImages" } });
        SetupMods(modMock.Object);

        using var exampleContent = CreateExampleContentStream();
        await _fileImporter.ImportAsync(exampleContent, "test.png");
        byte[] content = _fileSystem.File.ReadAllBytes("/ExampleModImages/test.png");

        // Copy extensions stated by mods should be used when importing files,
        // so the file should be copied to ExampleModImages
        Assert.Equal(ExampleFileContent, content);
    }

    [Fact]
    public async Task ShouldUtiliseCopyExtension()
    {
        using var exampleContent = CreateExampleContentStream();
        await _fileImporter.ImportAsync(exampleContent, "test.qsaber");
        byte[] content = _fileSystem.File.ReadAllBytes("Sabers/test.qsaber");

        // The built-in copy extensions contains an entry for .qsaber that should copy it to the path above
        Assert.Equal(ExampleFileContent, content);
    }

    [Fact]
    public async Task ShouldFailWithMultipleCopyExtensions()
    {
        var modMock = new Mock<IMod>();
        modMock
            .SetupGet(m => m.Id)
            .Returns("example-mod");
        modMock.SetupGet(m => m.CopyExtensions)
            .Returns(new Dictionary<string, string> { { "qsaber", "/AnotherQSaberMod/" } });

        SetupMods(modMock.Object);

        using var exampleContent = CreateExampleContentStream();
        var result = await _fileImporter.TryImportAsync(exampleContent, "test.qsaber");

        // Multiple copy extensions just triggers failure at the moment
        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldAddPlaylist()
    {
        var examplePlaylist = Util.ExamplePlaylist;
        const string expectedPlaylistId = "Example_Playlist";

        _playlistServiceMock
            .Setup(p => p.AddPlaylistAsync(It.IsAny<Playlist>()))
            .ReturnsAsync(expectedPlaylistId);

        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        var result = await _fileImporter.TryImportAsync(playlistStream, "test.bplist");

        // Make sure that the playlist was added to the playlist service
        _playlistServiceMock.Verify(p => p.AddPlaylistAsync(
            It.Is<Playlist>(v => v.PlaylistTitle == examplePlaylist.PlaylistTitle)),
            Times.Once()
        );
        Assert.Equal(expectedPlaylistId, result.ImportedPlaylistId);
    }

    [Fact]
    public async Task ShouldDownloadPlaylistSongs()
    {
        const string songHash = "12345";

        var examplePlaylist = Util.ExamplePlaylist;
        examplePlaylist.Songs = ImmutableList.Create(
            new BPSong(songHash, null, null)
        );

        var song = Song.CreateBlank(songHash);
        _songServiceMock.Setup(s =>
                s.ImportSongAsync(It.IsNotNull<ISongProvider>(), It.IsNotNull<string>()))
            .ReturnsAsync(new FileImportResult
            {
                ImportedSong = song,
                Type = FileImportResultType.Song
            });

        _beatSaverServiceMock.Setup(b => b.DownloadSongByHash(songHash))
            .ReturnsAsync(CreateEmptyZipStream());

        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        await _fileImporter.TryImportAsync(playlistStream, "my-playlist.json");

        // Make sure that the song was imported and downloaded
        _songServiceMock.Verify(s =>
                s.ImportSongAsync(It.IsNotNull<ISongProvider>(), It.IsNotNull<string>()),
            Times.Once);
        _beatSaverServiceMock.Verify(b => b.DownloadSongByHash(songHash), Times.Once);
    }

    [Fact]
    public async Task ShouldNotFailImportIfSongDownloadFails()
    {
        const string expectedPlaylistId = "Example_Playlist";

        var examplePlaylist = Util.ExamplePlaylist;
        examplePlaylist.Songs = ImmutableList.Create(
            new BPSong("", null, null)
        );

        _beatSaverServiceMock.Setup(b => b.DownloadSongByHash(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException());

        _playlistServiceMock.Setup(p => p.AddPlaylistAsync(It.IsAny<Playlist>()))
            .ReturnsAsync(expectedPlaylistId);

        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        var result = await _fileImporter.TryImportAsync(playlistStream, "my-playlist.json");

        // The song download should have been attempted, but this should not have failed the playlist import
        Assert.Equal(expectedPlaylistId, result.ImportedPlaylistId);
        _beatSaverServiceMock.Verify(b => b.DownloadSongByHash(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ShouldPreferKeyOverHash()
    {
        const string mapKey = "ff9";

        var examplePlaylist = Util.ExamplePlaylist;
        examplePlaylist.Songs = ImmutableList.Create(
            new BPSong("", null, mapKey)
        );

        _beatSaverServiceMock.Setup(b => b.DownloadSongByKey(It.IsAny<string>()))
            .ReturnsAsync(CreateEmptyZipStream());

        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        await _fileImporter.TryImportAsync(playlistStream, "my-playlist.json");

        // Verify that the song was downloaded by key, and NOT by hash
        _beatSaverServiceMock.Verify(b => b.DownloadSongByKey(mapKey), Times.Once);
        _beatSaverServiceMock.Verify(b => b.DownloadSongByHash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ShouldFailWithUnknownExtension()
    {
        using var blankContent = new MemoryStream();

        var result = await _fileImporter.TryImportAsync(blankContent, "my-file.unknown");
        // Since .unknown files are not supported, importing them should fail
        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ImportShouldThrowIfFailed()
    {
        using var blankContent = new MemoryStream();
        await Assert.ThrowsAsync<ImportException>(() =>
            _fileImporter.ImportAsync(blankContent, "example.unknown"));
    }
}