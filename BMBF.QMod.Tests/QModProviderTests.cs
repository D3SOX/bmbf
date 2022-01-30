using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.ModManagement;
using Moq;
using QuestPatcher.QMod;
using Xunit;
using Version = SemanticVersioning.Version;

namespace BMBF.QMod.Tests
{
    public class QModProviderTests : IDisposable
    {
        private readonly QModProvider _provider;
        private readonly IFileSystem _fileSystem = new MockFileSystem();
        
        public QModProviderTests()
        {
            _provider = Util.CreateProvider(
                new HttpClient(Mock.Of<HttpClientHandler>()),
                _fileSystem
            );
        }
        
        [Theory]
        [InlineData(QModProvider.ModExtension, true)]
        [InlineData(".zip", false)]
        [InlineData("", false)]
        public void ShouldBeImportable(string fileExtension, bool expected)
        {
            Assert.Equal(expected, _provider.CanAttemptImport($"my-file{fileExtension}"));
        }
        
        [Fact]
        public async Task ShouldInvokeModLoaded()
        {
            IMod? addedMod = null;
            _provider.ModLoaded += (_, mod) => addedMod = mod;

            using var modStream = Util.CreateTestingMod();
            var parsedMod = await _provider.ParseAndAddMod(modStream);
            
            Assert.Equal(parsedMod, addedMod);
        }

        [Fact]
        public async Task ShouldThrowWithInvalidMod()
        {
            using var emptyStream = new MemoryStream();
            await Assert.ThrowsAsync<InstallationException>(async () =>
                await _provider.TryParseModAsync(emptyStream));
        }

        [Theory]
        [InlineData(true, true, true, true)] // If all mod files exist, the mod should be marked as installed
        // If any are missing, then the mod is uninstalled
        [InlineData(false, true, true, false)] 
        [InlineData(true, false, true, false)]
        [InlineData(true, true, false, false)]
        public async Task ShouldSetModStatus(bool modFileExists, bool libFileExists, bool fileCopyExists, bool installed)
        {
            const string modPath = "libtest-mod.so";
            const string libPath = "libtest-lib.so";
            const string fileCopyPath = "myFile.txt";
            const string fileCopyDestination = "myFiles/myFile.txt";
            
            var fileSystem = new MockFileSystem();
            // Create the files in the mock file system
            if(modFileExists) fileSystem.AddFile($"/mods/{modPath}", MockFileData.NullObject);
            if(libFileExists) fileSystem.AddFile($"/libs/{libPath}", MockFileData.NullObject);
            if(fileCopyExists) fileSystem.AddFile(fileCopyDestination, MockFileData.NullObject);

            using var provider = Util.CreateProvider(new HttpClient(), fileSystem);

            var modStream = Util.CreateTestingMod(m =>
            {
                using var emptyStream = new MemoryStream();
                m.CreateModFileAsync(modPath, emptyStream);
                m.CreateLibraryFileAsync(libPath, emptyStream);
                m.AddFileCopyAsync(new FileCopy(fileCopyPath, fileCopyDestination), emptyStream);
            }, false);

            var result = await provider.ParseAndAddMod(modStream);
            Assert.Equal(installed, result.Installed);
        }

        [Fact]
        public async Task ShouldFailWithIncorrectPackageId()
        {
            var mod = Util.CreateTestingMod(m => m.PackageId = "com.imposter.app");
            await Assert.ThrowsAsync<InstallationException>(async () => await _provider.TryParseModAsync(mod));
        }
        
        [Fact]
        public async Task ShouldRemoveExistingMod()
        {
            // Loading a new mod with the same ID should uninstall the old mod
            var existingMod = await _provider.ParseAndAddMod(Util.CreateTestingMod());
            string? deletedModId = null;
            _provider.ModUnloaded += (_, args) => { deletedModId = args; };
            await _provider.ParseAndAddMod(Util.CreateTestingMod());

            Assert.Equal(existingMod.Id, deletedModId);
        }

        [Theory]
        [InlineData("^1.0.0", "1.1.0", true)] // If the dependants are still compatible, they should still be installed
        [InlineData("^1.0.0", "2.0.0", false)] // Otherwise, they should not have been reinstalled
        public async Task ShouldUninstallIncompatibleDependants(string versionRange, string newlyInstalled, bool shouldBeInstalled)
        {
            var dependantStream = Util.CreateTestingMod(m =>
            {
                m.Id = "dependant-mod";
                m.Dependencies.Add(new Dependency("test-mod", versionRange));
            });

            await _provider.ParseAndAddMod(Util.CreateTestingMod());

            var dependant = await _provider.ParseAndAddMod(dependantStream);
            await dependant.InstallAsync();
            await _provider.ParseAndAddMod(Util.CreateTestingMod(m => m.Version = Version.Parse(newlyInstalled)));

            Assert.Equal(shouldBeInstalled, dependant.Installed);
        }

        [Fact]
        public async Task ShouldInvokeModUnloaded()
        {
            var mod = await _provider.ParseAndAddMod(Util.CreateTestingMod());

            string? removedMod = null;
            _provider.ModUnloaded += (_, args) => removedMod = args;
            
            await _provider.UnloadModAsync(mod);
            Assert.Equal(mod.Id, removedMod);
        }

        [Fact]
        public async Task ShouldUninstallBeforeModRemoved()
        {
            var mod = await _provider.ParseAndAddMod(Util.CreateTestingMod());
            await mod.InstallAsync();
            
            // Since the mod as installed when it was unloaded, it should have been uninstalled before the unload
            await _provider.UnloadModAsync(mod);
            Assert.False(mod.Installed);
        }

        public void Dispose()
        {
            _provider.Dispose();
        }
    }
}