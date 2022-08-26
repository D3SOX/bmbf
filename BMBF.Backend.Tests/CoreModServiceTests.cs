using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.Backend.Implementations;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.ModManagement;
using BMBF.Resources;
using Moq;
using Xunit;
using Version = SemanticVersioning.Version;

namespace BMBF.Backend.Tests;

public class CoreModServiceTests : IDisposable
{
    private readonly Mock<IModService> _modServiceMock = new();
    private readonly Mock<IAssetService> _assetServiceMock = new();
    private readonly Mock<IBeatSaberService> _beatSaberServiceMock = new();
    private readonly Dictionary<string, (IMod mod, string path)> _installedMods = new();
    private readonly CoreModService _coreModService;

    private CoreMod ExampleCoreMod =>
        new("example-mod", "1.0.0", new Uri("https://example.com/coreMod"), "example-mod.mod");

    public CoreModServiceTests()
    {
        _modServiceMock.Setup(m => m.GetModsAsync())
            .ReturnsAsync(_installedMods);

        _coreModService = new CoreModService(_assetServiceMock.Object,
            _modServiceMock.Object,
            _beatSaberServiceMock.Object,
            Util.CreateMockProgressService());
    }

    private void AddMod(IMod mod)
    {
        _installedMods[mod.Id] = (mod, "exampleModPath");
    }

    private void SetupCoreMods(Dictionary<string, CoreMods> coreMods, bool downloaded)
    {
        _assetServiceMock.Setup(a => a.GetCoreMods())
            .ReturnsAsync((coreMods, downloaded));
    }

    private void SetupCoreMods(string version, bool downloaded, params CoreMod[] coreMods) => SetupCoreMods(
        new Dictionary<string, CoreMods>
        {
            [version] = new("unknown", coreMods.ToList())
        },
        downloaded
    );

    private void SetupBeatSaberVersion(string version)
    {
        _beatSaberServiceMock.Setup(b => b.GetInstallationInfoAsync())
            .ReturnsAsync(new InstallationInfo(version, 0, null, "/myApk.apk"));
    }

    private Mock<IMod> CreateModMock(string id, Version version, bool installed)
    {
        var modMock = new Mock<IMod>();
        modMock.SetupGet(m => m.Id)
            .Returns(id);
        modMock.SetupGet(m => m.Version)
            .Returns(version);
        modMock.SetupGet(m => m.Installed)
            .Returns(installed);

        return modMock;
    }

    private Mock<IMod> CreateModMock(CoreMod mod, bool installed)
    {
        var modMock = new Mock<IMod>();
        modMock.SetupGet(m => m.Id)
            .Returns(mod.Id);
        modMock.SetupGet(m => m.Version)
            .Returns(mod.Version);
        modMock.SetupGet(m => m.Installed)
            .Returns(installed);

        return modMock;
    }

    private void SetupCoreModDownload(CoreMod mod, Stream download)
    {
        _assetServiceMock.Setup(a => a.ExtractOrDownloadCoreMod(mod))
            .ReturnsAsync(download);
    }

    private Mock<IMod> SetupModImport(CoreMod mod, byte[] expectedContent)
    {
        var modMock = CreateModMock(mod, false);

        _modServiceMock.Setup(m => m.TryImportModAsync(
                It.Is<Stream>(s => s.CanSeek && Util.IsStreamContentEqual(s, expectedContent)),
                mod.FileName))
            .ReturnsAsync(new FileImportResult
            {
                ImportedMod = modMock.Object,
                Type = FileImportResultType.Mod
            });

        return modMock;
    }

    [Fact]
    public async Task ShouldReinstallUninstalledCoreMod()
    {
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");
        // Add an existing uninstalled core mod
        // The core mod manager should always make sure that uninstalled core mods are reinstalled
        var modMock = CreateModMock(coreMod, false);
        AddMod(modMock.Object);
        var result = await _coreModService.InstallAsync(false);

        modMock.Verify(m => m.InstallAsync(), Times.Once);
        Assert.Equal(coreMod, result.Installed.Single());
    }

    [Fact]
    public async Task ShouldDownloadMissingCoreMods()
    {
        using var exampleContent = Util.CreateExampleContentStream();
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");
        SetupCoreModDownload(coreMod, exampleContent);
        SetupModImport(coreMod, Util.ExampleFileContent);

        var result = await _coreModService.InstallAsync(false);

        // The core mod should have been downloaded as it did not exist already
        _modServiceMock.Verify(m => m.TryImportModAsync(
                It.IsAny<Stream>(),
                coreMod.FileName), Times.Once
        );
        Assert.Single(result.Added.Where(m => m.Id == coreMod.Id && m.Version == coreMod.Version));
    }

    [Fact]
    public async Task ShouldDownloadOutdatedCoreMod()
    {
        using var exampleContent = Util.CreateExampleContentStream();
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");
        SetupCoreModDownload(coreMod, exampleContent);
        SetupModImport(coreMod, Util.ExampleFileContent);

        // The existing core mod has a newer version that what currently exists
        var existingMod = CreateModMock(coreMod.Id, Version.Parse("0.9.0"), true);
        AddMod(existingMod.Object);

        var result = await _coreModService.InstallAsync(false);

        // The newer core mod should have been installed (overwriting the older mod)
        _modServiceMock.Verify(m =>
                m.TryImportModAsync(It.IsAny<Stream>(),
                    It.IsAny<string>()),
            Times.Once
        );
        Assert.Single(result.Added.Where(m => m.Id == coreMod.Id && m.Version == coreMod.Version));
    }

    [Fact]
    public async Task ShouldNotDowngradeCoreMod()
    {
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");

        // Add an existing mod with a *newer* version than specified in the core mod index
        var existingMod = CreateModMock(coreMod.Id, Version.Parse("1.1.0"), true);
        AddMod(existingMod.Object);

        // The older core mod should NOT be downloaded
        // This is deliberate as core mod developers may install newer versions of their core mods during testing
        // We don't want to overwrite those, it would be very inconvenient
        var result = await _coreModService.InstallAsync(false);
        Assert.Empty(result.Added);
    }

    [Fact]
    public async Task ShouldAddToFailedToFetchIfDownloadThrows()
    {
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");

        _assetServiceMock.Setup(a => a.ExtractOrDownloadCoreMod(coreMod))
            .ThrowsAsync(new HttpRequestException());

        var result = await _coreModService.InstallAsync(false);
        // Make sure that this core mod failing to be fetched will be reported back to the caller
        Assert.Equal(coreMod, result.FailedToFetch.Single());
    }

    [Fact]
    public async Task ShouldAddToFailedToInstallIfImportFails()
    {
        using var exampleContent = Util.CreateExampleContentStream();
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");
        SetupCoreModDownload(coreMod, exampleContent);
        _modServiceMock.Setup(m => m.TryImportModAsync(exampleContent, It.IsAny<string>()))
            .ReturnsAsync(new FileImportResult
            {
                Type = FileImportResultType.Failed
            });

        // Importing the core mod failed, which should NOT cause installing core mods to throw, but should
        // instead add it to the FailedToInstall collection
        var result = await _coreModService.InstallAsync(false);
        Assert.Equal(coreMod, result.FailedToInstall.Single());
    }

    [Fact]
    public async Task ShouldAddToFailedToInstallIfParseFails()
    {
        using var exampleContent = Util.CreateExampleContentStream();
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");
        SetupCoreModDownload(coreMod, exampleContent);
        _modServiceMock.Setup(m => m.TryImportModAsync(exampleContent, It.IsAny<string>()))
            .ReturnsAsync((FileImportResult?) null);

        // Parsing the core mod failed, which should NOT cause installing core mods to throw, but should
        // instead add it to the FailedToInstall collection
        var result = await _coreModService.InstallAsync(false);
        Assert.Equal(coreMod, result.FailedToInstall.Single());
    }

    [Fact]
    public async Task ShouldAddToFailedToInstallIfInstallFails()
    {
        using var exampleContent = Util.CreateExampleContentStream();
        var coreMod = ExampleCoreMod;
        SetupCoreMods("1.0.0", true, coreMod);
        SetupBeatSaberVersion("1.0.0");
        SetupCoreModDownload(coreMod, exampleContent);
        var modMock = SetupModImport(coreMod, Util.ExampleFileContent);
        modMock.Setup(m => m.InstallAsync())
            .ThrowsAsync(new InstallationException("Example failure"));

        var result = await _coreModService.InstallAsync(false);
        // Installing the core mod failed, which should NOT cause installing core mods to throw, but should
        // instead add it to the FailedToInstall collection
        Assert.Equal(coreMod, result.FailedToInstall.Single());
    }

    [Fact]
    public async Task ShouldFailIfBeatSaberNotInstalled()
    {
        // Core mods cannot be installed if Beat Saber is not, as the Beat Saber version to install them for is unknown
        var result = await _coreModService.InstallAsync(false);
        Assert.Equal(CoreModResultType.BeatSaberNotInstalled, result.ResultType);
    }

    [Theory]
    // If the versions match, and the core mod index was downloaded from online, then UsedDownloaded is expected
    [InlineData("1.0.0", "1.0.0", true, CoreModResultType.UsedDownloaded)]
    // The versions also match, but built-in core mods were used, so UsedBuiltIn
    [InlineData("1.0.0", "1.0.0", false, CoreModResultType.UsedBuiltIn)]
    // No core mods for the installed BS version were found in the core mod index, so we are certain that no core mods
    // exist for the current version - NoneAvailableForVersion
    [InlineData("1.0.0", "1.1.0", true, CoreModResultType.NoneAvailableForVersion)]
    // No core mods were built-in for the version, but we couldn't fetch the index from online to check if any
    // core mods have now been added for the version - NoneBuiltInForVersion
    [InlineData("1.0.0", "1.1.0", false, CoreModResultType.NoneBuiltInForVersion)]
    public async Task ShouldReportCorrectIndexType(string versionWithCoreMods,
        string beatSaberVersion,
        bool indexWasDownloaded,
        CoreModResultType expected)
    {
        SetupCoreMods(versionWithCoreMods, indexWasDownloaded);
        SetupBeatSaberVersion(beatSaberVersion);

        var result = await _coreModService.InstallAsync(false);
        Assert.Equal(expected, result.ResultType);
    }

    [Fact]
    public async Task ShouldPreferCachedDownloadedIndex()
    {
        var coreMod = ExampleCoreMod;
        SetupBeatSaberVersion("1.0.0");
        var modMock = CreateModMock(coreMod, false);
        AddMod(modMock.Object);
        SetupCoreMods("1.0.0", true, coreMod);

        // Install core mods once, this will cache the core mod index
        await _coreModService.InstallAsync(true);

        // Simulate the internet no longer being available - so the built-in index will be returned from IAssetService
        SetupCoreMods("1.0.0", false, coreMod);
        var postRefreshInstall = await _coreModService.InstallAsync(true);

        // The downloaded index should be cached and used instead of the built-in index (even though we passed true for refresh)
        Assert.Equal(CoreModResultType.UsedDownloaded, postRefreshInstall.ResultType);
    }

    [Fact]
    public async Task ShouldRefreshOnlineIndexIfConfigured()
    {
        var coreMod = ExampleCoreMod;
        SetupBeatSaberVersion("1.0.0");
        var modMock = CreateModMock(coreMod, false);
        AddMod(modMock.Object);
        SetupCoreMods("0.9.0", true, coreMod);

        var initialInstall = await _coreModService.InstallAsync(true);
        SetupCoreMods("1.0.0", true, coreMod);
        var postRefreshInstall = await _coreModService.InstallAsync(true);

        // Initially, there were no mods available for the Beat Saber version
        Assert.Equal(CoreModResultType.NoneAvailableForVersion, initialInstall.ResultType);
        // However, in the second instance, the available core mods changed, and as we passed true to refresh,
        // the core mod index should have been re-downloaded
        // Hence, mods for the current BS version should be found
        Assert.Equal(CoreModResultType.UsedDownloaded, postRefreshInstall.ResultType);
    }

    public void Dispose()
    {
        _coreModService.Dispose();
    }
}
