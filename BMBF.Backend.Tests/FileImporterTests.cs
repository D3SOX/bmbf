using System.Collections.Concurrent;
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
using BMBF.Backend.Services;
using BMBF.Backend.Util.BPList;
using BMBF.ModManagement;
using BMBF.Resources;
using Moq;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BMBF.Backend.Tests;

public class FileImporterTests
{
    private static readonly byte[] ExampleFileContent = Encoding.UTF8.GetBytes("Hello World!");
    private Playlist ExamplePlaylist => new(
        "Example Playlist",
        "Unicorns",
        "Example",
        ImmutableList<BPSong>.Empty,
        null
    );
    
    /// <summary>
    /// Creates a <see cref="FileImporter"/> with either stubbed mocks, or the given override mocks.
    /// </summary>
    /// <returns>The instantiated file importer</returns>
    private FileImporter Create(ISongService? overrideSongService = null,
        IPlaylistService? overridePlaylistService = null,
        IBeatSaverService? overrideBeatSaverService = null,
        BMBFSettings? overrideSettings = null,
        IAssetService? overrideAssetService = null,
        IFileSystem? overrideFileSystem = null,
        List<IMod>? overrideMods = null)
    {
        var assetMock = new Mock<IAssetService>();
        assetMock.Setup(m => m.GetExtensions())
        .ReturnsAsync(() => new FileExtensions(
            new Dictionary<string, string> { { "qsaber", "/Sabers" } },
            new List<string> { "json", "bplist" },
            new List<string> { "json", "yml" }
        ));

        var modServiceMock = new Mock<IModService>();
        modServiceMock.Setup(m => m.GetModsAsync())
            .ReturnsAsync(overrideMods?.ToDictionary(m => m.Id, m => (m, "")) ?? new Dictionary<string, (IMod mod, string path)>());
            

        return new(
            overrideSongService ?? Mock.Of<ISongService>(),
            overridePlaylistService ?? Mock.Of<IPlaylistService>(),
            overrideBeatSaverService ?? Mock.Of<IBeatSaverService>(),
            overrideSettings ?? new BMBFSettings
            {
                ConfigsPath = "/Configs"
            },
            modServiceMock.Object,
            overrideAssetService ?? assetMock.Object,
            overrideFileSystem ?? new MockFileSystem());
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
        
        var songServiceMock = new Mock<ISongService>();
        songServiceMock.Setup(s => 
            s.ImportSongAsync(It.IsNotNull<ZipArchive>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResult);
        
        var fileImporter = Create(songServiceMock.Object);
        var result = await fileImporter.ImportAsync(songStream, "myFile.zip");
        
        // Make sure that the result returns matches that returned by the ISongService
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ShouldErrorWithInvalidArchive()
    {
        // Blank stream will not be a valid archive
        using var modStream = new MemoryStream();
    
        var fileImporter = Create();
        var result = await fileImporter.TryImportAsync(modStream, "myFile.zip");
        
        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldErrorWhenCannotLoadExtensions()
    {
        var assetServiceMock = new Mock<IAssetService>();
        assetServiceMock.Setup(a => a.GetExtensions()).ThrowsAsync(new HttpRequestException());
        
        var fileImporter = Create(overrideAssetService: assetServiceMock.Object);
        using var blankStream = new MemoryStream();
        // QSABER is arbitrary, this can be any file type that is NOT .ZIP
        var result = await fileImporter.TryImportAsync(blankStream, "myFile.qsaber");

        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldNotImportUnknownConfig()
    {
        var fileImporter = Create();

        using var blankStream = new MemoryStream();
        // YAML is used instead of JSON here, since we don't want to trigger playlist importing
        var result = await fileImporter.TryImportAsync(blankStream, "myUnknownConfig.yml");
        
        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldImportKnownConfig()
    {
        var modMock = new Mock<IMod>();
        modMock.SetupGet(m => m.Id)
            .Returns("example-mod");
        var mod = modMock.Object;
        
        // Add an example mod which will allow a config with its ID to be imported
        var modServiceMock = new Mock<IModService>();
        modServiceMock.Setup(m => m.GetModsAsync())
            .ReturnsAsync(new Dictionary<string, (IMod mod, string path)>
            {
                {mod.Id, (mod, "")}
            });

        var fileSystem = new MockFileSystem();
        var fileImporter = Create(overrideMods: new List<IMod>{ modMock.Object }, overrideFileSystem: fileSystem);
        
        // Import a config with the mod ID as the filename, and read the content placed in the config path
        using var exampleContent = CreateExampleContentStream();
        await fileImporter.TryImportAsync(exampleContent, $"{mod.Id}.json");
        byte[] content = fileSystem.File.ReadAllBytes("/Configs/example-mod.json");

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
        
        var fileSystem = new MockFileSystem();
        var fileImporter = Create(overrideMods: new List<IMod>{ modMock.Object }, overrideFileSystem: fileSystem);

        using var exampleContent = CreateExampleContentStream();
        await fileImporter.ImportAsync(exampleContent, "test.png");
        byte[] content = fileSystem.File.ReadAllBytes("/ExampleModImages/test.png");

        // Copy extensions stated by mods should be used when importing files,
        // so the file should be copied to ExampleModImages
        Assert.Equal(ExampleFileContent, content);
    }

    [Fact]
    public async Task ShouldUtiliseCopyExtension()
    {
        var fileSystem = new MockFileSystem();
        var fileImporter = Create(overrideFileSystem: fileSystem);
        
        using var exampleContent = CreateExampleContentStream();
        await fileImporter.ImportAsync(exampleContent, "test.qsaber");
        byte[] content = fileSystem.File.ReadAllBytes("Sabers/test.qsaber");
        
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
        
        var fileImporter = Create(overrideMods: new List<IMod>{ modMock.Object });

        using var exampleContent = CreateExampleContentStream();
        var result = await fileImporter.TryImportAsync(exampleContent, "test.qsaber");
        
        // Multiple copy extensions just triggers failure at the moment
        Assert.Equal(FileImportResultType.Failed, result.Type);
    }

    [Fact]
    public async Task ShouldAddPlaylist()
    {
        var examplePlaylist = ExamplePlaylist;
        const string expectedPlaylistId = "Example_Playlist";
        
        var playlistServiceMock = new Mock<IPlaylistService>();
        playlistServiceMock
            .Setup(p => p.AddPlaylistAsync(It.IsAny<Playlist>()))
            .ReturnsAsync(expectedPlaylistId);
        var fileImporter = Create(overridePlaylistService: playlistServiceMock.Object);

        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        var result = await fileImporter.TryImportAsync(playlistStream, "test.bplist");
        
        // Make sure that the playlist was added to the playlist service
        playlistServiceMock.Verify(p => p.AddPlaylistAsync(
            It.Is<Playlist>(v => v.PlaylistTitle == examplePlaylist.PlaylistTitle)),
            Times.Once()
        );
        Assert.Equal(expectedPlaylistId, result.ImportedPlaylistId);
    }

    [Fact]
    public async Task ShouldDownloadPlaylistSongs()
    {
        const string songHash = "12345";
        
        var examplePlaylist = ExamplePlaylist;
        examplePlaylist.Songs = ImmutableList.Create(
            new BPSong(songHash, null, null)
        );

        var songServiceMock = new Mock<ISongService>();
        var song = Song.CreateBlank();
        song.Hash = songHash;
        songServiceMock.Setup(s => 
                s.ImportSongAsync(It.IsNotNull<ZipArchive>(), It.IsNotNull<string>()))
            .ReturnsAsync(new FileImportResult
            {
                ImportedSong = song,
                Type = FileImportResultType.Song
            });
        songServiceMock.Setup(s => s.GetSongsAsync())
            .ReturnsAsync(new ConcurrentDictionary<string, Song>());
        
        var beatSaverMock = new Mock<IBeatSaverService>();
        beatSaverMock.Setup(b => b.DownloadSongByHash(songHash))
            .Returns((string _) => Task.FromResult(CreateEmptyZipStream())!);

        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        var fileImporter = Create(overrideBeatSaverService: beatSaverMock.Object,
            overrideSongService: songServiceMock.Object);
        await fileImporter.TryImportAsync(playlistStream, "my-playlist.json");

        // Make sure that the song was imported and downloaded
        songServiceMock.Verify(s =>
                s.ImportSongAsync(It.IsNotNull<ZipArchive>(), It.IsNotNull<string>()),
            Times.Once);
        beatSaverMock.Verify(b => b.DownloadSongByHash(songHash), Times.Once);
    }

    [Fact]
    public async Task ShouldNotFailImportIfSongDownloadFails()
    {
        const string expectedPlaylistId = "Example_Playlist";

        var examplePlaylist = ExamplePlaylist;
        examplePlaylist.Songs = ImmutableList.Create(
            new BPSong("", null, null)
        );

        var beatSaverMock = new Mock<IBeatSaverService>();
        beatSaverMock.Setup(b => b.DownloadSongByHash(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException());

        var songServiceMock = new Mock<ISongService>();
        songServiceMock.Setup(s => s.GetSongsAsync())
            .ReturnsAsync(new ConcurrentDictionary<string, Song>());

        var playlistServiceMock = new Mock<IPlaylistService>();
        playlistServiceMock.Setup(p => p.AddPlaylistAsync(It.IsAny<Playlist>()))
            .ReturnsAsync(expectedPlaylistId);
        
        var fileImporter = Create(overrideBeatSaverService: beatSaverMock.Object,
            overrideSongService: songServiceMock.Object,
            overridePlaylistService: playlistServiceMock.Object);
        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        var result = await fileImporter.TryImportAsync(playlistStream, "my-playlist.json");
        
        // The song download should have been attempted, but this should not have failed the playlist import
        Assert.Equal(expectedPlaylistId, result.ImportedPlaylistId);
        beatSaverMock.Verify(b => b.DownloadSongByHash(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ShouldPreferKeyOverHash()
    {
        const string mapKey = "ff9";
        
        var examplePlaylist = ExamplePlaylist;
        examplePlaylist.Songs = ImmutableList.Create(
            new BPSong("", null, mapKey)
        );
        
        var songServiceMock = new Mock<ISongService>();
        songServiceMock.Setup(s => s.GetSongsAsync())
            .ReturnsAsync(new ConcurrentDictionary<string, Song>());
        
        var beatSaverMock = new Mock<IBeatSaverService>();
        beatSaverMock.Setup(b => b.DownloadSongByKey(It.IsAny<string>()))
            .ReturnsAsync(CreateEmptyZipStream());
        
        var fileImporter = Create(overrideBeatSaverService: beatSaverMock.Object, overrideSongService: songServiceMock.Object);
        using var playlistStream = CreatePlaylistStream(examplePlaylist);
        await fileImporter.TryImportAsync(playlistStream, "my-playlist.json");
        
        // Verify that the song was downloaded by key, and NOT by hash
        beatSaverMock.Verify(b => b.DownloadSongByKey(mapKey), Times.Once);
        beatSaverMock.Verify(b => b.DownloadSongByHash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ShouldFailWithUnknownExtension()
    {
        var fileImporter = Create();
        using var blankContent = new MemoryStream();
        
        var result = await fileImporter.TryImportAsync(blankContent, "my-file.unknown");
        // Since .unknown files are not supported, importing them should fail
        Assert.Equal(FileImportResultType.Failed, result.Type);
    }
    
}