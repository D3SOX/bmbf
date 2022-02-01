using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.ModManagement;
using QuestPatcher.QMod;
using RichardSzalay.MockHttp;
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
                new MockHttpMessageHandler().ToHttpClient(),
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

            using var modStream = await Util.CreateTestingModAsync();
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
            if (modFileExists) fileSystem.AddFile($"/mods/{modPath}", MockFileData.NullObject);
            if (libFileExists) fileSystem.AddFile($"/libs/{libPath}", MockFileData.NullObject);
            if (fileCopyExists) fileSystem.AddFile(fileCopyDestination, MockFileData.NullObject);

            using var provider = Util.CreateProvider(new HttpClient(), fileSystem);

            var modStream = await Util.CreateTestingModAsync(m =>
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
            using var modStream =  await Util.CreateTestingModAsync(m => m.PackageId = "com.imposter.app");
            await Assert.ThrowsAsync<InstallationException>(async () => await _provider.TryParseModAsync(modStream));
        }

        [Fact]
        public async Task ShouldRemoveExistingMod()
        {
            using var modStream = await Util.CreateTestingModAsync();
            // Loading a new mod with the same ID should uninstall the old mod
            var existingMod = await _provider.ParseAndAddMod(modStream);
            string? deletedModId = null;
            _provider.ModUnloaded += (_, args) => { deletedModId = args; };
            
            using var duplicateModStream = await Util.CreateTestingModAsync();
            await _provider.ParseAndAddMod(duplicateModStream);

            Assert.Equal(existingMod.Id, deletedModId);
        }

        [Theory]
        [InlineData("^1.0.0", "1.1.0", true)] // If the dependants are still compatible, they should still be installed
        [InlineData("^1.0.0", "2.0.0", false)] // Otherwise, they should not have been reinstalled
        public async Task ShouldUninstallIncompatibleDependants(string versionRange, string newlyInstalled, bool shouldBeInstalled)
        {
            // This mod will depend on our example mod
            using var dependantStream = await Util.CreateTestingModAsync(m =>
            {
                m.Id = "dependant-mod";
                m.Dependencies.Add(new Dependency("test-mod", versionRange));
            });

            // This older dependency will be within its version range
            using var oldDependencyStream = await Util.CreateTestingModAsync();
            await _provider.ParseAndAddMod(oldDependencyStream);

            var dependant = await _provider.ParseAndAddMod(dependantStream);
            await dependant.InstallAsync(); // Mark the mod as installed

            // This new dependency will NOT match the version range,
            // so adding it to the provider should uninstall the dependant
            using var newDependencyStream = await Util.CreateTestingModAsync(m => m.Version = Version.Parse(newlyInstalled));
            await _provider.ParseAndAddMod(newDependencyStream);

            Assert.Equal(shouldBeInstalled, dependant.Installed);
        }

        [Fact]
        public async Task ShouldInvokeModUnloaded()
        {
            using var modStream = await Util.CreateTestingModAsync();
            var mod = await _provider.ParseAndAddMod(modStream);

            string? removedMod = null;
            _provider.ModUnloaded += (_, args) => removedMod = args;

            await _provider.UnloadModAsync(mod);
            Assert.Equal(mod.Id, removedMod);
        }

        [Fact]
        public async Task ShouldUninstallBeforeModRemoved()
        {
            using var modStream = await Util.CreateTestingModAsync();
            var mod = await _provider.ParseAndAddMod(modStream);
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
