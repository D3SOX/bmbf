using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.ModManagement;
using QuestPatcher.QMod;
using Xunit;
using Version = SemanticVersioning.Version;

namespace BMBF.QMod.Tests
{
    public class QModProviderTests
    {
        [Theory]
        [InlineData(QModProvider.ModExtension, true)]
        [InlineData(".zip", false)]
        [InlineData("", false)]
        public void ShouldBeImportable(string fileExtension, bool expected)
        {
            using var provider = Util.CreateProvider();
            Assert.Equal(expected, provider.CanAttemptImport($"my-file{fileExtension}"));
        }
        
        [Fact]
        public async Task ShouldInvokeModLoaded()
        {
            using var provider = Util.CreateProvider(new HttpClient(), new MockFileSystem());
            var resultStream = new MemoryStream();
            provider.ModLoaded += (_, args) =>
            {
                Assert.NotNull(args.Mod);
                args.Stream.CopyToAsync(resultStream);
            };

            var modStream = Util.CreateTestingMod();
            await provider.TryImportModAsync(modStream, "test-mod.qmod");
            
            Assert.Equal(resultStream.ToArray(), modStream.ToArray());
        }

        [Fact]
        public async Task ShouldThrowWithInvalidMod()
        {
            var emptyStream = new MemoryStream();
            using var provider = Util.CreateProvider();
            await Assert.ThrowsAsync<InstallationException>(async () =>
                await provider.TryImportModAsync(emptyStream, "my-mod.qmod"));
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

            IMod? result = null;
            provider.ModLoaded += (_, args) => result = args.Mod;
            await provider.TryImportModAsync(modStream, "my-mod.qmod");
            Assert.Equal(installed, result?.Installed);
        }

        [Fact]
        public async Task ShouldFailWithIncorrectPackageId()
        {
            var mod = Util.CreateTestingMod(m => m.PackageId = "com.imposter.app");
            using var provider = Util.CreateProvider();
            await Assert.ThrowsAsync<InstallationException>(async () => await provider.TryImportModAsync(mod, "my-mod.qmod"));
        }
        
        [Fact]
        public async Task ShouldRemoveExistingMod()
        {
            using var provider = Util.CreateProvider();

            // Loading a new mod with the same ID should uninstall the old mod
            var existingMod = await provider.TryImportModAsync(Util.CreateTestingMod(), "existing-mod.qmod");
            IMod? deletedMod = null;
            provider.ModUnloaded += (_, args) => { deletedMod = args; };
            await provider.TryImportModAsync(Util.CreateTestingMod(), "new-mod.qmod");

            Assert.Equal(existingMod, deletedMod);
        }

        [Theory]
        [InlineData("^1.0.0", "1.1.0", true)] // If the dependants are still compatible, they should still be installed
        [InlineData("^1.0.0", "2.0.0", false)] // Otherwise, they should not have been reinstalled
        public async Task ShouldUninstallIncompatibleDependants(string versionRange, string newlyInstalled, bool shouldBeInstalled)
        {
            using var provider = Util.CreateProvider();
            var dependantStream = Util.CreateTestingMod(m =>
            {
                m.Id = "dependant-mod";
                m.Dependencies.Add(new Dependency("test-mod", versionRange));
            });

            await provider.TryImportModAsync(Util.CreateTestingMod(), "existing-mod.qmod");
            
            var dependant = await provider.TryImportModAsync(dependantStream, "dependant.qmod") ?? throw new NullReferenceException();
            await dependant.InstallAsync();
            await provider.TryImportModAsync(Util.CreateTestingMod(m => m.Version = Version.Parse(newlyInstalled)), "new-mod.qmod");

            Assert.Equal(shouldBeInstalled, dependant.Installed);
        }

        [Fact]
        public async Task ShouldInvokeModUnloaded()
        {
            using var provider = Util.CreateProvider();
            var mod = await provider.TryImportModAsync(Util.CreateTestingMod(), "my-mod.qmod") ?? throw new NullReferenceException();

            IMod? removedMod = null;
            provider.ModUnloaded += (_, args) => removedMod = args;
            
            await provider.UnloadModAsync(mod);
            Assert.Equal(mod, removedMod);
        }

        [Fact]
        public async Task ShouldUninstallBeforeModRemoved()
        {
            using var provider = Util.CreateProvider();
            var mod = await provider.TryImportModAsync(Util.CreateTestingMod(), "my-mod.qmod") ?? throw new NullReferenceException();
            await mod.InstallAsync();
            
            // Since the mod as installed when it was unloaded, it should have been uninstalled before the unload
            await provider.UnloadModAsync(mod);
            Assert.False(mod.Installed);
        }
    }
}