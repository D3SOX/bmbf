using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using BMBF.ModManagement;
using QuestPatcher.QMod;
using Xunit;
using Version = SemanticVersioning.Version;

namespace BMBF.QMod.Tests
{
    public class QModTests
    {
        private const string DependencyId = "my-dependency";

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldHaveCopiedFilesAfterInstall(bool modInstalled)
        {
            const string modFileName = "libtest-mod.so";
            const string libFileName = "libtest-lib.so";
            const string fileCopyName = "myFile.txt";
            const string fileCopyPath = "myFiles/myFile.txt";

            var modStream = Util.CreateTestingMod(m =>
            {
                var emptyStream = new MemoryStream();
                // Create empty mod files, lib files, and file copies
                m.CreateModFileAsync(modFileName, emptyStream);
                m.CreateLibraryFileAsync(libFileName, emptyStream);
                m.AddFileCopyAsync(new FileCopy(fileCopyName, fileCopyPath), emptyStream);
            }, false);

            var fileSystem = new MockFileSystem();
            var provider = Util.CreateProvider(null, fileSystem);

            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();
            await mod.InstallAsync();
            if (!modInstalled)
            {
                await mod.UninstallAsync();
            }

            // If the mod is installed, then the mod's files should be copied, otherwise none of them should exist
            Assert.Equal(modInstalled, fileSystem.FileExists($"mods/{modFileName}"));
            Assert.Equal(modInstalled, fileSystem.FileExists($"libs/{libFileName}"));
            Assert.Equal(modInstalled, fileSystem.FileExists(fileCopyPath));
        }

        [Fact]
        public async Task LibraryFileShouldNotBeDeletedWhenStillUsed()
        {
            const string libFileName = "my-library.so";
            
            var fileSystem = new MockFileSystem();
            var provider = Util.CreateProvider(null, fileSystem);
            var otherModStream = Util.CreateTestingMod(m =>
            {
                m.Id = "other-mod";
                m.CreateLibraryFileAsync(libFileName, new MemoryStream());
            });
            
            var modStream = Util.CreateTestingMod(m =>
            {
                m.CreateLibraryFileAsync(libFileName, new MemoryStream());
            });
            
            var otherMod = await provider.TryImportModAsync(otherModStream, "my-mod.qmod") ?? throw new NullReferenceException();
            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();
            await mod.InstallAsync();
            await otherMod.InstallAsync();

            // As otherMod is still installed and has a libFile with the same name, it shouldn't be deleted.
            await mod.UninstallAsync();
            Assert.True(fileSystem.FileExists($"libs/{libFileName}"));
        }

        [Fact]
        public async Task ShouldBeInstalledAfterInstall()
        {
            var provider = Util.CreateProvider();
            var mod = await provider.TryImportModAsync(Util.CreateTestingMod(), "my-mod.qmod") ?? throw new NullReferenceException();
            await mod.InstallAsync();
            Assert.True(mod.Installed);
        }
        
        [Fact]
        public async Task ShouldBeUninstalledAfterUninstall()
        {
            var provider = Util.CreateProvider();
            var mod = await provider.TryImportModAsync(Util.CreateTestingMod(), "my-mod.qmod") ?? throw new NullReferenceException();
            await mod.InstallAsync();
            await mod.UninstallAsync();
            Assert.False(mod.Installed);
        }

        [Fact]
        public async Task ShouldInstallDependencies()
        {
            var dependencyStream = Util.CreateTestingMod(m => m.Id = DependencyId);
            // Make sure to mock the dependency download
            var provider = Util.CreateProvider(Util.CreateHttpClientMock(dependencyStream));
            var modStream = Util.CreateTestingMod(m =>
            {
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0", "https://example.com/my-dependency.qmod"));
            });

            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();

            QMod? installedDep = null;
            provider.ModLoaded += (_, args) => installedDep = (QMod) args.Mod;
            await mod.InstallAsync();

            // Verify that the dependency is installed and has the correct ID
            Assert.Equal(DependencyId, installedDep?.Id);
            Assert.True(installedDep?.Installed);
        }

        [Fact]
        public async Task ShouldReinstallUninstalledDependencies()
        {
            var modStream = Util.CreateTestingMod(m =>
            {
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0"));
            });
            var dependencyStream = Util.CreateTestingMod(m => m.Id = DependencyId);
            var provider = Util.CreateProvider();
            var dependency = await provider.TryImportModAsync(dependencyStream, "my-dependency.qmod") ?? throw new NullReferenceException();
            // The dependency is loaded, but not installed
            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();

            await mod.InstallAsync();
            // The dependency should have been reinstalled, since it wasn't installed during mod installation
            Assert.True(dependency.Installed);
        }

        [Fact]
        public async Task ShouldUpgradeDependencies()
        {
            // Mod depends on version ^1.0.0
            var modStream = Util.CreateTestingMod(m =>
            {
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0", "https://example.com/my-dependency.qmod"));
            });
            // Version 0.5.0 does not intersect this, so the dependency should be upgraded
            var oldDepStream = Util.CreateTestingMod(m =>
            {
                m.Id = DependencyId;
                m.Version = Version.Parse("0.5.0");
            });
            // The downloadIfMissing will link to this version
            var newDepStream = Util.CreateTestingMod(m => m.Id = DependencyId); 
            
            // Create an HttpClient that will mock the dependency download and inject it into our provider
            var provider = Util.CreateProvider(Util.CreateHttpClientMock(newDepStream));
            var existingDependency = await provider.TryImportModAsync(oldDepStream, "my-dependency.qmod") ?? throw new NullReferenceException();
            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();
            
            QMod? uninstalledDependency = null;
            provider.ModUnloaded += (_, args) => uninstalledDependency = (QMod) args;
            await mod.InstallAsync();
            
            // The older dependency should have been uninstalled, as its version does not intersect the required version
            Assert.Equal(existingDependency, uninstalledDependency);
        }

        [Fact]
        public async Task ShouldFailWithNoDownloadUri()
        {
            // Mod depends on version ^1.0.0
            var modStream = Util.CreateTestingMod(m =>
            {
                // But does not state a download link to get the dependency if missing
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0"));
            });
            var provider = Util.CreateProvider();
            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();

            await Assert.ThrowsAsync<InstallationException>(async () => await mod.InstallAsync());
        }

        [Fact]
        public async Task ShouldFailOnUpgradeWithNoDownloadUri()
        {
            // Mod depends on version ^1.0.0
            var modStream = Util.CreateTestingMod(m => m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0", "https://example.com/my-dependency.qmod")));
            // Version 0.5.0 does not intersect this, so the dependency needs to be upgraded
            var oldDepStream = Util.CreateTestingMod(m =>
            {
                m.Id = DependencyId;
                m.Version = Version.Parse("0.5.0");
            });
            var provider = Util.CreateProvider();
            await provider.TryImportModAsync(oldDepStream, "my-dependency.qmod");
            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();

            // But there is no download URI, so the install should throw 
            await Assert.ThrowsAsync<InstallationException>(async () => await mod.InstallAsync());
        }

        [Fact]
        public async Task ShouldUninstallLibraryWithNoDependants()
        {
            var libStream = Util.CreateTestingMod(m =>
            {
                m.Id = DependencyId;
                m.IsLibrary = true;
            });
            var modStream = Util.CreateTestingMod(m => m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0")));
            var provider = Util.CreateProvider();
            var lib = await provider.TryImportModAsync(libStream, "my-lib.qmod") ?? throw new NullReferenceException();
            var mod = await provider.TryImportModAsync(modStream, "my-mod.qmod") ?? throw new NullReferenceException();

            // Libraries should be automatically uninstalled if no installed mods depend on them
            await mod.InstallAsync(); // Library will be installed as mod depends on it
            await mod.UninstallAsync(); // Library will be uninstalled as mod no longer depends on it
            Assert.False(lib.Installed);
        }
    }
}