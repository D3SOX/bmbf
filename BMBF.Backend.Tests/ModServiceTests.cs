using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Implementations;
using BMBF.Backend.Models;
using BMBF.ModManagement;
using Moq;
using Xunit;

namespace BMBF.Backend.Tests;

public class ModServiceTests : IDisposable
{
    private const string FileExtension = "exampleModType";
    private const string ExampleModId = "example-mod";

    public ModServiceTests()
    {
        _providerMock.Setup(s =>
                s.CanAttemptImport(It.Is<string>(fileName =>
                    Path.GetExtension(fileName).Substring(1) == FileExtension)))
            .Returns(true);

        var toDispose = new List<IMod>();
        _providerMock.Setup(p => p.TryParseModAsync(It.IsNotNull<Stream>(), It.IsAny<bool>()))
            .Returns((Stream stream, bool leaveOpen) =>
            {
                // Create a new mod to simulate the parse
                var modMock = new Mock<IMod>();
                modMock.SetupGet(m => m.Id).Returns(ExampleModId);
                // Dispose the underlying stream if the mod is disposed
                if (!leaveOpen)
                {
                    modMock.Setup(m => m.Dispose())
                        .Callback(stream.Dispose);
                }

                return Task.FromResult<IMod?>(modMock.Object);
            });

        // Once a mod is added to the provider, the provider owns it
        // and we must dispose it when the provider is disposed
        _providerMock.Setup(p => p.AddModAsync(It.IsAny<IMod>()))
            .Callback((IMod mod) => toDispose.Add(mod));

        _providerMock.Setup(p => p.UnloadModAsync(It.IsAny<IMod>()))
            .Callback((IMod mod) =>
            {
                mod.Dispose();
                _providerMock.Raise(p => p.ModUnloaded += null, _providerMock, mod.Id);
            });

        // Dispose all underlying mods when the provider is disposed
        _providerMock.Setup(p => p.Dispose())
            .Callback(() =>
            {
                foreach (var modStream in toDispose)
                {
                    modStream.Dispose();
                }
            });

        _modService = new ModService(_settings, _fileSystem);
        _modService.RegisterProvider(_providerMock.Object);
    }

    private readonly ModService _modService;
    private readonly IFileSystem _fileSystem = new MockFileSystem();
    private readonly BMBFSettings _settings = new()
    {
        RootDataPath = Path.GetFullPath("/BMBFData"),
        ModsDirectoryName = "Mods"
    };

    private string ModsPath => Path.Combine(_settings.RootDataPath, _settings.ModsDirectoryName);

    private readonly Mock<IModProvider> _providerMock = new();

    private readonly byte[] _exampleModContent = System.Text.Encoding.UTF8.GetBytes("Hello Mods!");

    private Stream CreateExampleContentStream()
    {
        var stream = new MemoryStream();
        stream.Write(_exampleModContent);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ShouldNotImportIfNoProviderWithExtension()
    {
        using var modStream = CreateExampleContentStream();
        var result = await _modService.TryImportModAsync(modStream, "example.unknown");

        // The import result should be null (and NOT an error) since the file importer may now attempt to import this
        // file as another type (e.g. copy extension, playlist)
        Assert.Null(result);
    }

    [Fact]
    public async Task ShouldNotImportIfNotParsed()
    {
        using var modStream = CreateExampleContentStream();

        _providerMock.Setup(p => p.TryParseModAsync(
            It.IsNotNull<Stream>(),
            It.IsAny<bool>()))
            .Returns(Task.FromResult<IMod?>(null));

        var result = await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");

        _providerMock.Verify(p => p.TryParseModAsync(It.IsNotNull<Stream>(), It.IsAny<bool>()), Times.Once);
        // The import result should be null (and NOT an error) since the file importer may now attempt to import this
        // file as another type (e.g. copy extension, playlist)
        Assert.Null(result);
    }

    [Fact]
    public async Task ShouldFailIfParseThrows()
    {
        using var modStream = CreateExampleContentStream();
        _providerMock.Setup(s => s.TryParseModAsync(
                It.IsNotNull<Stream>(),
                It.IsAny<bool>()))
            .ThrowsAsync(new InstallationException("Failed to parse mod"));

        var result = await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");
        Assert.Equal(FileImportResultType.Failed, result?.Type);
    }

    [Fact]
    public async Task ShouldFailIfAddThrows()
    {
        using var modStream = CreateExampleContentStream();

        _providerMock.Setup(s => s.AddModAsync(It.IsNotNull<IMod>()))
            .ThrowsAsync(new InstallationException("Failed to add mod"));

        var result = await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");

        _providerMock.Verify(p => p.AddModAsync(It.IsNotNull<IMod>()), Times.Once);
        Assert.Equal(FileImportResultType.Failed, result?.Type);
    }

    [Fact]
    public async Task ShouldAddMod()
    {
        using var modStream = CreateExampleContentStream();

        IMod? modFromAddEvent = null;
        _modService.ModAdded += (_, mod) => modFromAddEvent = mod;
        var result = await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");

        var mods = await _modService.GetModsAsync();
        var modInCollection = mods.Values.Single().mod;
        var importedMod = result?.ImportedMod;

        Assert.Equal(ExampleModId, importedMod?.Id);
        Assert.Equal(importedMod, modInCollection);
        Assert.Equal(importedMod, modFromAddEvent);
    }

    [Fact]
    public async Task ModPathShouldBeWithinModsFolder()
    {
        using var modStream = CreateExampleContentStream();
        await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");
        var mods = await _modService.GetModsAsync();

        var modPath = mods.Values.Single().path;
        Assert.Equal(ModsPath, Path.GetDirectoryName(modPath));
    }

    [Fact]
    public async Task CachedModDataShouldMatch()
    {
        using var modStream = CreateExampleContentStream();

        await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");
        _modService.Dispose(); // Dispose to guarantee that streams get closed and the cache data is written

        // Mod file content should match that saved to the mods folder
        var modPath = (await _modService.GetModsAsync()).Values.Single().path;
        Assert.Equal(_exampleModContent, _fileSystem.File.ReadAllBytes(modPath));
    }

    [Fact]
    public async Task ShouldDeleteMod()
    {
        using var modStream = CreateExampleContentStream();
        await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");
        var mods = await _modService.GetModsAsync();
        var modPair = mods.Values.Single();

        string? deletedModId = null;
        _modService.ModRemoved += (_, m) => deletedModId = m;

        await _modService.UnloadModAsync(modPair.mod);

        Assert.Empty(mods);
        Assert.False(_fileSystem.File.Exists(modPair.path)); // Verify that the mod was cleared from the mods folder too
        Assert.Equal(ExampleModId, deletedModId);
    }

    [Fact]
    public async Task ShouldBroadcastModStatusChanges()
    {
        using var modStream = CreateExampleContentStream();
        await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");

        var mods = await _modService.GetModsAsync();
        var mod = mods.Values.Single().mod;
        bool broadcasted = false;
        _modService.ModStatusChanged += (_, _) => broadcasted = true;

        // Raise a status change for a mod on the provider
        _providerMock.Raise(p => p.ModStatusChanged += null, _providerMock.Object, mod);

        // Should be mirrored in the mod service
        Assert.True(broadcasted);
    }

    [Fact]
    public async Task ShouldLoadNewMods()
    {
        var mods = await _modService.GetModsAsync();
        var newModPath = Path.Combine(ModsPath, $"new-mod.{FileExtension}");

        _fileSystem.File.WriteAllBytes(newModPath, _exampleModContent);
        await _modService.LoadNewModsAsync();

        var mod = mods.Values.Single();
        Assert.Equal(mod.path, newModPath);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldDeleteInvalidMods(bool deleteInvalids)
    {
        _settings.DeleteInvalidMods = deleteInvalids;
        _providerMock.Setup(p => p.CanAttemptImport(It.IsAny<string>()))
            .Returns(false);
        // .unknown is not a file type accepted by our provider
        var modPath = Path.Combine(ModsPath, "invalid-mod.unknown");
        _fileSystem.Directory.CreateDirectory(ModsPath);
        _fileSystem.File.WriteAllBytes(modPath, _exampleModContent);

        await _modService.LoadNewModsAsync();

        // If deleting invalid mods is enabled, then the mod file should not exist (and vice-versa)
        Assert.Equal(!deleteInvalids, _fileSystem.File.Exists(modPath));
    }

    [Fact]
    public async Task ShouldUseFirstAvailableProvider()
    {
        var additionalProviderMock = new Mock<IModProvider>();
        _modService.RegisterProvider(additionalProviderMock.Object);

        using var modStream = CreateExampleContentStream();
        await _modService.TryImportModAsync(modStream, $"example.{FileExtension}");

        additionalProviderMock.Verify(p => p.TryParseModAsync(It.IsAny<Stream>(), It.IsAny<bool>()), Times.Never);
        _providerMock.Verify(p => p.TryParseModAsync(It.IsAny<Stream>(), It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ShouldUseGivenProvider()
    {
        // When using the internal import (used for dependencies), we need to make sure that the passed provider is 
        // used (instead of any other registered providers)
        var modManager = (IModManager)_modService;
        var additionalProviderMock = new Mock<IModProvider>();
        additionalProviderMock.Setup(p => p.TryParseModAsync(It.IsAny<Stream>(), It.IsAny<bool>()))
            .ThrowsAsync(new InstallationException("Example failure")); // Fail during import to prove it's the correct provider
        _modService.RegisterProvider(additionalProviderMock.Object);

        using var modStream = CreateExampleContentStream();

        // Make sure that importing is using the import method from the provider
        await Assert.ThrowsAsync<InstallationException>(async () =>
            await modManager.ImportMod(additionalProviderMock.Object, modStream, $"example-mod.{FileExtension}"));
    }

    [Fact]
    public async Task ShouldAddModWithInternalImport()
    {
        var modManager = (IModManager)_modService;
        using var modStream = CreateExampleContentStream();

        IMod? modFromAddEvent = null;
        _modService.ModAdded += (_, mod) => modFromAddEvent = mod;
        var mod = await modManager.ImportMod(_providerMock.Object,
            modStream,
            $"example-mod.{FileExtension}"
        );

        var mods = await _modService.GetModsAsync();
        var modInCollection = mods.Values.Single().mod;

        // Key mod import checks, make sure that the mod matches our example mod ID
        Assert.Equal(ExampleModId, mod.Id);
        Assert.Equal(mod, modInCollection);
        Assert.Equal(mod, modFromAddEvent);
    }

    [Fact]
    public async Task InternalImportShouldNotLock()
    {
        var modManager = (IModManager)_modService;
        using var modStream = CreateExampleContentStream();

        // Simulate this import operation being part of a larger mod operation by locking the install lock
        await modManager.InstallLock.WaitAsync();

        await modManager.ImportMod(_providerMock.Object,
            modStream,
            $"example-mod.{FileExtension}"
        );
    }

    public void Dispose()
    {
        _modService.Dispose();
    }
}