using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Models.Setup;
using BMBF.Resources;
using Microsoft.Extensions.FileProviders;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace BMBF.Backend.Tests;

public class AssetServiceTests
{
    private const string PackageId = "com.beatgames.beatsaber";
    

    private AssetService _assetService;
    private readonly MockHttpMessageHandler _messageHandler = new();
    private readonly Mock<IFileProvider> _fileProviderMock = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Set up some example resource URIs to use
    // Note that we don't have to worry about these sites potentially existing as we mock the HttpClient
    private readonly BMBFResources _resourceUris = new()
    {
        CoreModsIndex = new Uri("https://example.com/coremodsindex"),
        ModLoaderVersion = new Uri("https://example.com/modloaderversion"),
        LibUnityIndex = new Uri("https://example.com/libunityindex"),
        LibUnityVersionTemplate = "https://example.com/libunity/{0}",
        DeltaIndex = new Uri("https://example.com/deltaindex"),
        DeltaVersionTemplate = "https://example.com/delta/{0}",
        ExtensionsIndex = new Uri("https://example.com/extensions")
    };

    private readonly BuiltInAssets _builtInAssets = new()
    {
        CoreMods = new List<CoreMod>
        {
            new("example",
                "1.0.0",
                new Uri("https://example.com/examplecoremod"),
                "example.qmod")
        },
        BeatSaberVersion = "1.0.0",
        ModLoaderVersion = "1.0.0"
    };
    
    private readonly FileExtensions _exampleExtensions =  new(new Dictionary<string, string>(),
        new List<string>
        {
            "ExampleExtension"
        }, 
        new List<string>());

    private readonly byte[] _modloaderContent = Encoding.UTF8.GetBytes("Example modloader content");
    private readonly byte[] _mainContent = Encoding.UTF8.GetBytes("Example main content");


    public AssetServiceTests()
    {
        _fileProviderMock.Setup(f => f.GetFileInfo(It.IsNotNull<string>()))
            .Returns(Mock.Of<IFileInfo>());
        
        _assetService = CreateAssetService();
    }

    private AssetService CreateAssetService()
    {
        var indexStream = new MemoryStream();
        JsonSerializer.SerializeAsync(indexStream, _builtInAssets, _serializerOptions);
        indexStream.Position = 0;
        
        SetupFile(AssetService.IndexPath, indexStream);
        
        return new AssetService(_fileProviderMock.Object,
            _messageHandler.ToHttpClient(),
            new BMBFSettings
            {
                PackageId = PackageId
            },
            _resourceUris
        );
    }

    /// <summary>
    /// Sets up the the file at <paramref name="path"/> on the asset file provider to return <paramref name="content"/>
    /// </summary>
    /// <param name="path">Path of the file to set up</param>
    /// <param name="content">Stream containing the file's content</param>
    private void SetupFile(string path, Stream content)
    {
        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.SetupGet(f => f.Exists)
            .Returns(true);
        fileInfoMock.Setup(f => f.CreateReadStream())
            .Returns(content);

        _fileProviderMock.Setup(f => f.GetFileInfo(path))
            .Returns(fileInfoMock.Object);
    }
    
    /// <summary>
    /// Sets up the modloader version URL to return the given object as JSON
    /// </summary>
    /// <param name="version">Mod loader version to return</param>
    private void SetupModLoaderVersion(ModLoaderVersion version)
    {
        _messageHandler.When(_resourceUris.ModLoaderVersion.ToString())
            .Respond(new StringContent(JsonSerializer.Serialize(
                    version,
                    _serializerOptions)
                )
            );
    }

    /// <summary>
    /// Sets up the built-in 32 or 64 bit modloader with <see cref="_mainContent"/> and <see cref="_modloaderContent"/>
    /// </summary>
    /// <param name="use64Bit">Whether to create the 32 bit or 64 bit modloader files</param>
    private void SetupBuiltInModLoader(bool use64Bit)
    {
        SetupFile(use64Bit ? AssetService.Main64Path : AssetService.Main32Path, Util.CopyToMemoryStream(_mainContent));
        SetupFile(use64Bit ? AssetService.ModLoader64Path : AssetService.ModLoader32Path, Util.CopyToMemoryStream(_modloaderContent));
    }

    /// <summary>
    /// Checks to see if <paramref name="modloader"/> and <paramref name="main"/> contain <see cref="_mainContent"/> and
    /// <see cref="_modloaderContent"/> respectively.
    /// </summary>
    /// <param name="modloader">Modloader stream</param>
    /// <param name="main">libmain stream</param>
    private void AssertIsCorrectModLoader(Stream modloader, Stream main)
    {
        Util.AssertStreamContainsContent(modloader, _modloaderContent);
        Util.AssertStreamContainsContent(main, _mainContent);
    }

    [Fact]
    public async Task ShouldPreferDownloadedCoreModIndex()
    {
        var coreModIndex = new Dictionary<string, CoreMods>
        {
            ["1.0.0"] = new("unknown", new List<CoreMod>()),
            ["2.0.0"] = new("unknown", new List<CoreMod>())
        };

        _messageHandler.When(_resourceUris.CoreModsIndex.ToString())
            .Respond("application/json", JsonSerializer.Serialize(coreModIndex, _serializerOptions));

        var result = await _assetService.GetCoreMods();
        Assert.Equal(coreModIndex.Count, result?.coreMods.Count);
        Assert.True(result?.downloaded);
    }

    [Fact]
    public async Task ShouldUseIntegratedCoreModIndexAsBackup()
    {
        // If downloading the core mods index fails (which it will as we haven't set up the download), AssetService
        // should use the built-in core mods index
        var result = (await _assetService.GetCoreMods())!.Value;
        Assert.False(result.downloaded);
        Assert.Single(result.coreMods);
        Assert.Equal(_builtInAssets.CoreMods?.First().Id,
            result.coreMods[_builtInAssets.BeatSaberVersion!].Mods.First().Id);
    }

    [Fact]
    public async Task CoreModIndexShouldBeNullIfNoWayToFetch()
    {
        // Remove built-in core mods
        _builtInAssets.CoreMods = null;
        _builtInAssets.BeatSaberVersion = null;
        
        // Recreate the asset service (necessary as it parses the index in the constructor)
        _assetService = CreateAssetService();
        
        // Core mods index should be empty instead of throwing
        var result = await _assetService.GetCoreMods();
        Assert.Null(result);
    }

    [Fact]
    public async Task ShouldExtractCoreModIfExists()
    {
        var coreMod = _builtInAssets.CoreMods!.First();
        using var modContent = new MemoryStream();
        SetupFile(Path.Combine(AssetService.CoreModsFolder, coreMod.FileName), modContent);

        using var coreModStream = await _assetService.ExtractOrDownloadCoreMod(coreMod);
        
        // Since the core mod exists within the built-in core mods, it should extract it from assets instead of downlaoding
        Assert.Equal(modContent, coreModStream);
    }

    [Fact]
    public async Task ShouldDownloadCoreModIfMissing()
    {
        // Setup a core mod which is NOT contained within the built-in core mods
        var coreMod = new CoreMod("other-mod",
            "1.0.0",
            new Uri("https://other-mod.com"),
            "other-mod.qmod");
        // Setup the data to download
        using var modContent = Util.CreateExampleContentStream();
        _messageHandler.When(coreMod.DownloadLink.ToString())
            .Respond("application/octet-stream", modContent);
        
        using var coreModStream = await _assetService.ExtractOrDownloadCoreMod(coreMod);
        
        Util.AssertIsExampleContent(coreModStream);
    }

    [Fact]
    public async Task ShouldDownloadDelta()
    {
        var diffInfo = new DiffInfo("1.0.0", "0.9.0", "Example");
        _messageHandler.When(string.Format(_resourceUris.DeltaVersionTemplate, diffInfo.Name))
            .Respond(new ByteArrayContent(Util.ExampleFileContent));

        // Deltas will always be downloaded, built-in deltas aren't supported
        await using var result = await _assetService.GetDelta(diffInfo, default);
        Util.AssertIsExampleContent(result);
    }

    [Fact]
    public async Task ReturnedDeltaShouldBeSeekable()
    {
        var diffInfo = new DiffInfo("1.0.0", "0.9.0", "Example");
        _messageHandler.When(string.Format(_resourceUris.DeltaVersionTemplate, diffInfo.Name))
            .Respond(new ByteArrayContent(Util.ExampleFileContent));
        
        await using var result = await _assetService.GetDelta(diffInfo, default);
        
        // Since octodiff doesn't support non-seekable deltas, we need to make sure the returned stream is seekable
        Assert.True(result.CanSeek);
    }

    [Fact]
    public async Task ShouldThrowIfDeltaNotFound()
    {
        var diffInfo = new DiffInfo("1.0.0", "0.9.0", "Example");
        await Assert.ThrowsAsync<HttpRequestException>(async () => await _assetService.GetDelta(diffInfo, default));
    }

    [Fact]
    public async Task ShouldReturnCorrectDiffsFromIndex()
    {
        var diffs = new List<DiffInfo>
        {
            new("1.0.0", "0.9.0", "Example")
        };

        _messageHandler.When(_resourceUris.DeltaIndex.ToString())
            .Respond(new StringContent(JsonSerializer.Serialize(diffs, _serializerOptions)));

        var returnedDiffs = await _assetService.GetDiffs();

        Assert.Equal(diffs.Count, returnedDiffs.Count);
        // Make sure that every returned diff matches the original diffs collection
        for (int i = 0; i < diffs.Count; i++)
        {
            var originalDiff = diffs[i];
            var returnedDiff = returnedDiffs[i];

            Assert.True(originalDiff.Name == returnedDiff.Name
                        && originalDiff.FromVersion == returnedDiff.FromVersion &&
                        originalDiff.ToVersion == returnedDiff.ToVersion);   
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldUseBuiltInModLoaderIfVersionMatches(bool use64Bit)
    {
        SetupBuiltInModLoader(use64Bit);
        SetupModLoaderVersion(new ModLoaderVersion("1.0.0", null!, null!, null!, null!));
        
        var modLoader = await _assetService.GetModLoader(use64Bit, default);
        AssertIsCorrectModLoader(modLoader.modloader, modLoader.main);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldUseBuiltInModLoaderIfCannotDownloadVersion(bool use64Bit)
    {
        SetupBuiltInModLoader(use64Bit);
        SetupModLoaderVersion(new ModLoaderVersion("1.0.0", null!, null!, null!, null!));

        var modLoader = await _assetService.GetModLoader(use64Bit, default);
        AssertIsCorrectModLoader(modLoader.modloader, modLoader.main);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldDownloadModLoaderIfVersionDoesNotMatch(bool use64Bit)
    {
        var modloaderLinks = new ModLoaderVersion(
            "1.2.3",
            new Uri("https://example.com/modloader32"),
            new Uri("https://example.com/main32"),
            new Uri("https://example.com/modloader64"),
            new Uri("https://example.com/main64")
        );
        
        SetupModLoaderVersion(modloaderLinks);
        _messageHandler.When((use64Bit ? modloaderLinks.Main64 : modloaderLinks.Main32).ToString())
            .Respond(new ByteArrayContent(_mainContent));
        _messageHandler.When((use64Bit ? modloaderLinks.ModLoader64 : modloaderLinks.ModLoader32).ToString())
            .Respond(new ByteArrayContent(_modloaderContent));

        var modLoader = await _assetService.GetModLoader(use64Bit, default);
        AssertIsCorrectModLoader(modLoader.modloader, modLoader.main);
    }

    [Fact]
    public async Task ShouldUseBuiltInUnityIfAvailable()
    {
        using var exampleContent = Util.CreateExampleContentStream();
        SetupFile(AssetService.UnityPath, exampleContent);

        await using var unityStream = await _assetService.GetLibUnity(_builtInAssets.BeatSaberVersion!, default);
        
        Util.AssertIsExampleContent(exampleContent);
    }

    [Fact]
    public async Task ShouldReturnNullIfNoUnityForAppVersion()
    {
        var unityIndex = new Dictionary<string, Dictionary<string, string>>
        {
            [PackageId] = new()
        };
        _messageHandler.When(_resourceUris.LibUnityIndex.ToString())
            .Respond(new StringContent(JsonSerializer.Serialize(unityIndex, _serializerOptions)));

        await using var unityStream = await _assetService.GetLibUnity("2.0.0", default);
        Assert.Null(unityStream);
    }

    [Fact]
    public async Task ShouldReturnMatchingStreamIfVersionMatches()
    {
        const string exampleUnityVersion = "2019.4.28f1";
        
        var unityIndex = new Dictionary<string, Dictionary<string, string>>
        {
            [PackageId] = new()
            { 
                ["2.0.0"] = exampleUnityVersion
            }
        };    
        
        _messageHandler.When(_resourceUris.LibUnityIndex.ToString())
            .Respond(new StringContent(JsonSerializer.Serialize(unityIndex, _serializerOptions)));
        _messageHandler.When(string.Format(_resourceUris.LibUnityVersionTemplate, exampleUnityVersion))
            .Respond(new ByteArrayContent(Util.ExampleFileContent));
        
        await using var unityStream = await _assetService.GetLibUnity("2.0.0", default);
        
        Assert.NotNull(unityStream);
        Util.AssertIsExampleContent(unityStream!);
    }

    private void AssertAreExampleExtensions(FileExtensions extensions)
    {
        Assert.Equal(_exampleExtensions.ConfigExtensions, extensions.ConfigExtensions);
        Assert.Equal(_exampleExtensions.CopyExtensions, extensions.CopyExtensions);
        Assert.Equal(_exampleExtensions.PlaylistExtensions, extensions.PlaylistExtensions);
    }

    [Fact]
    public async Task ShouldPreferDownloadedExtensions()
    {
        _messageHandler.When(_resourceUris.ExtensionsIndex.ToString())
            .Respond(new StringContent(JsonSerializer.Serialize(_exampleExtensions, _serializerOptions)));

        var extensions = await _assetService.GetExtensions();
        AssertAreExampleExtensions(extensions);
    }

    [Fact]
    public async Task ShouldUseBuiltInExtensionsIfCannotDownload()
    {
        using var tempStream = new MemoryStream();
        JsonSerializer.Serialize(tempStream, _exampleExtensions, _serializerOptions);
        tempStream.Position = 0;
        SetupFile(AssetService.ExtensionsPath, tempStream);
        
        var extensions = await _assetService.GetExtensions();
        AssertAreExampleExtensions(extensions);
    }
}
