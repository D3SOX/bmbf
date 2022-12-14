using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Threading.Tasks;
using BMBF.ModManagement;
using QuestPatcher.QMod;
using RichardSzalay.MockHttp;
using Xunit;
using Version = SemanticVersioning.Version;

namespace BMBF.QMod.Tests
{
    public class QModTests : IDisposable
    {
        private const string DependencyId = "my-dependency";

        private readonly QModProvider _provider;
        private readonly IFileSystem _fileSystem = new MockFileSystem();

        private readonly MockHttpMessageHandler _messageHandler = new();
        public QModTests()
        {
            _provider = Util.CreateProvider(
                _messageHandler.ToHttpClient(),
                _fileSystem
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldHaveCopiedFilesAfterInstall(bool modInstalled)
        {
            const string modFileName = "libtest-mod.so";
            const string libFileName = "libtest-lib.so";
            const string fileCopyName = "myFile.txt";
            const string fileCopyPath = "myFiles/myFile.txt";

            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                var emptyStream = new MemoryStream();
                // Create empty mod files, lib files, and file copies
                m.CreateModFileAsync(modFileName, emptyStream);
                m.CreateLibraryFileAsync(libFileName, emptyStream);
                m.AddFileCopyAsync(new FileCopy(fileCopyName, fileCopyPath), emptyStream);
            }, false);

            var mod = await _provider.ParseAndAddMod(modStream);
            await mod.InstallAsync();
            if (!modInstalled)
            {
                await mod.UninstallAsync();
            }

            // If the mod is installed, then the mod's files should be copied, otherwise none of them should exist
            Assert.Equal(modInstalled, _fileSystem.File.Exists($"mods/{modFileName}"));
            Assert.Equal(modInstalled, _fileSystem.File.Exists($"libs/{libFileName}"));
            Assert.Equal(modInstalled, _fileSystem.File.Exists(fileCopyPath));
        }

        [Fact]
        public async Task ShouldNotThrowIfModUnregistered()
        {
            using var modStream = await Util.CreateTestingModAsync();
            using var mod = await _provider.TryParseModAsync(modStream);
            if (mod == null) throw new FormatException("Invalid testing mod");

            await mod.InstallAsync();
            await mod.UninstallAsync();
        }

        [Fact]
        public async Task LibraryFileShouldNotBeDeletedWhenStillUsed()
        {
            const string libFileName = "my-library.so";

            using var otherModStream = await Util.CreateTestingModAsync(m =>
            {
                m.Id = "other-mod";
                m.CreateLibraryFileAsync(libFileName, new MemoryStream());
            });

            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                m.CreateLibraryFileAsync(libFileName, new MemoryStream());
            });

            var otherMod = await _provider.ParseAndAddMod(otherModStream);
            var mod = await _provider.ParseAndAddMod(modStream);
            await mod.InstallAsync();
            await otherMod.InstallAsync();

            // As otherMod is still installed and has a libFile with the same name, it shouldn't be deleted.
            await mod.UninstallAsync();
            Assert.True(_fileSystem.File.Exists($"libs/{libFileName}"));
        }

        [Fact]
        public async Task ShouldBeInstalledAfterInstall()
        {
            using var modStream = await Util.CreateTestingModAsync();
            var mod = await _provider.ParseAndAddMod(modStream);
            await mod.InstallAsync();
            Assert.True(mod.Installed);
        }

        [Fact]
        public async Task ShouldBeUninstalledAfterUninstall()
        {
            using var modStream = await Util.CreateTestingModAsync();
            var mod = await _provider.ParseAndAddMod(modStream);

            await mod.InstallAsync();
            await mod.UninstallAsync();
            Assert.False(mod.Installed);
        }

        [Fact]
        public async Task ShouldInstallDependencies()
        {
            using var dependencyStream = await Util.CreateTestingModAsync(m => m.Id = DependencyId);
            var dependencyUrl = "https://example.com/my-dependency.qmod";
            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0", dependencyUrl));
            });

            // Make sure to mock the dependency download
            _messageHandler.When(dependencyUrl)
                .Respond("application/octet-stream", dependencyStream);

            var mod = await _provider.ParseAndAddMod(modStream);

            QMod? installedDep = null;
            _provider.ModLoaded += (_, args) => installedDep = (QMod) args;
            await mod.InstallAsync();

            // Verify that the dependency is installed and has the correct ID
            Assert.Equal(DependencyId, installedDep?.Id);
            Assert.True(installedDep?.Installed);
        }

        [Fact]
        public async Task ShouldReinstallUninstalledDependencies()
        {
            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0"));
            });
            using var dependencyStream = await Util.CreateTestingModAsync(m => m.Id = DependencyId);
            var dependency = await _provider.ParseAndAddMod(dependencyStream);
            // The dependency is loaded, but not installed
            var mod = await _provider.ParseAndAddMod(modStream);

            await mod.InstallAsync();
            // The dependency should have been reinstalled, since it wasn't installed during mod installation
            Assert.True(dependency.Installed);
        }

        [Fact]
        public async Task ShouldThrowIfDependencyDownloadFails()
        {
            var dependencyUrl = "https://example.com/my-dependency.qmod";
            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0", dependencyUrl));
            });

            _messageHandler.When(dependencyUrl)
                .Respond(HttpStatusCode.NotFound);

            var mod = await _provider.ParseAndAddMod(modStream) ?? throw new InstallationException("Failed to parse mod");
            await Assert.ThrowsAsync<InstallationException>(async () => await mod.InstallAsync());
        }

        [Fact]
        public async Task ShouldUpgradeDependencies()
        {
            // Mod depends on version ^1.0.0
            var dependencyUrl = "https://example.com/my-dependency.qmod";
            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0", dependencyUrl));
            });
            // Version 0.5.0 does not intersect this, so the dependency should be upgraded
            using var oldDepStream = await Util.CreateTestingModAsync(m =>
            {
                m.Id = DependencyId;
                m.Version = Version.Parse("0.5.0");
            });
            // The downloadIfMissing will link to this version
            using var newDepStream = await Util.CreateTestingModAsync(m => m.Id = DependencyId);

            // Create an HttpClient that will mock the dependency download and inject it into our provider
            _messageHandler.When(dependencyUrl)
                .Respond("application/octet-stream", newDepStream);

            var existingDependency = await _provider.ParseAndAddMod(oldDepStream);
            var mod = await _provider.ParseAndAddMod(modStream);

            string? uninstalledDependency = null;
            _provider.ModUnloaded += (_, args) => uninstalledDependency = args;
            await mod.InstallAsync();

            // The older dependency should have been uninstalled, as its version does not intersect the required version
            Assert.Equal(existingDependency.Id, uninstalledDependency);
        }

        [Fact]
        public async Task ShouldFailWithNoDownloadUri()
        {
            // Mod depends on version ^1.0.0
            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                // But does not state a download link to get the dependency if missing
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0"));
            });
            var mod = await _provider.ParseAndAddMod(modStream);

            await Assert.ThrowsAsync<InstallationException>(async () => await mod.InstallAsync());
        }

        [Fact]
        public async Task ShouldFailOnUpgradeWithNoDownloadUri()
        {
            // Mod depends on version ^1.0.0
            using var modStream = await Util.CreateTestingModAsync(m =>
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0")));
            // Version 0.5.0 does not intersect this, so the dependency needs to be upgraded
            using var oldDepStream = await Util.CreateTestingModAsync(m =>
            {
                m.Id = DependencyId;
                m.Version = Version.Parse("0.5.0");
            });
            await _provider.ParseAndAddMod(oldDepStream);
            var mod = await _provider.ParseAndAddMod(modStream);

            // But there is no download URI, so the install should throw 
            await Assert.ThrowsAsync<InstallationException>(async () => await mod.InstallAsync());
        }

        [Theory]
        [InlineData("cover.png", "cover.png")]
        [InlineData("mySubDir/cover.png", "cover.png")]
        public async Task ShouldHaveCorrectCoverFileName(string coverPath, string fileName)
        {
            using var modStream = await Util.CreateTestingModAsync(m => m.WriteCoverImageAsync(coverPath, new MemoryStream()).Wait());

            var mod = await _provider.ParseAndAddMod(modStream);

            Assert.Equal(fileName, mod.CoverImageFileName);
        }

        [Fact]
        public async Task ShouldThrowWhenNoCover()
        {
            using var modStream = await Util.CreateTestingModAsync();

            var mod = await _provider.ParseAndAddMod(modStream);

            // This should throw InvalidOperationException, as no cover exists in the QMOD
            Assert.Throws<InvalidOperationException>(() => mod.OpenCoverImage());
        }

        [Fact]
        public async Task CoverContentShouldMatch()
        {
            const string content = "Hello World!";

            using var modStream = await Util.CreateTestingModAsync(m =>
            {
                using var coverStream = new MemoryStream();
                using var coverWriter = new StreamWriter(coverStream);
                coverWriter.WriteLine(content);
                coverWriter.Flush();
                coverStream.Position = 0;

                m.WriteCoverImageAsync("cover.png", coverStream).Wait();
            });

            var mod = await _provider.ParseAndAddMod(modStream);
            await using var coverStream = mod.OpenCoverImage();
            using var coverReader = new StreamReader(coverStream);
            Assert.Equal(content, await coverReader.ReadLineAsync());
        }

        [Fact]
        public async Task ShouldUninstallLibraryWithNoDependants()
        {
            using var libStream = await Util.CreateTestingModAsync(m =>
            {
                m.Id = DependencyId;
                m.IsLibrary = true;
            });
            using var modStream = await Util.CreateTestingModAsync(m =>
                m.Dependencies.Add(new Dependency(DependencyId, "^1.0.0")));
            var lib = await _provider.ParseAndAddMod(libStream);
            var mod = await _provider.ParseAndAddMod(modStream);

            // Libraries should be automatically uninstalled if no installed mods depend on them
            await mod.InstallAsync(); // Library will be installed as mod depends on it
            await mod.UninstallAsync(); // Library will be uninstalled as mod no longer depends on it
            Assert.False(lib.Installed);
        }

        public void Dispose()
        {
            _provider.Dispose();
        }
    }
}
