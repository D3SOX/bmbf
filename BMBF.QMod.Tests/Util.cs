using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BMBF.ModManagement;
using Moq;
using QuestPatcher.QMod;
using Version = SemanticVersioning.Version;

namespace BMBF.QMod.Tests
{
    /// <summary>
    /// Utility methods for QMod testing
    /// </summary>
    public static class Util
    {
        private const string PackageId = "com.beatgames.beatsaber";

        /// <summary>
        /// Creates a <see cref="QModProvider"/> with /mods and /libs paths and a default HttpClient and mock FileSystem
        /// </summary>
        /// <param name="httpClient">HTTP client to be used for dependency downloads</param>
        /// <param name="fileSystem">File system to simulate mod installs</param>
        /// <returns>Created <see cref="QModProvider"/></returns>
        public static QModProvider CreateProvider(HttpClient httpClient, IFileSystem fileSystem)
        {
            var modManagerMock = new Mock<IModManager>();

            var provider = new QModProvider(PackageId, "/mods", "/libs", httpClient, fileSystem, modManagerMock.Object);

            var installLock = new SemaphoreSlim(1);
            modManagerMock.SetupGet(m => m.InstallLock).Returns(installLock);
            modManagerMock.Setup(m => m.ImportMod(provider, It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(async delegate (IModProvider genericProvider, Stream stream, string _)
                    {
                        var modProvider = (QModProvider)genericProvider;

                        var mod = await modProvider.TryParseModAsync(stream) ?? throw new NullReferenceException();
                        await provider.AddModAsyncInternal((QMod)mod, new HashSet<string>());
                        return (QMod)mod;
                    }
                );

            return provider;
        }

        /// <summary>
        /// Creates an example mod for use during testing and saves it to a <see cref="MemoryStream"/>
        /// </summary>
        /// <param name="configureOptions">A delegate used to configure details of the mod</param>
        /// <param name="addFile">Whether or not to add an example file</param>
        /// <returns>Stream containing the mod's content</returns>
        public static async Task<MemoryStream> CreateTestingModAsync(Action<QuestPatcher.QMod.QMod>? configureOptions = null, bool addFile = true)
        {
            await using var modStream = new MemoryStream();
            await using (var mod = new QuestPatcher.QMod.QMod(
                             modStream,
                             "test-mod",
                             "Test Mod",
                             Version.Parse("1.0.0"),
                             PackageId,
                             "1.0.0",
                             "Unicorns"
                         ))
            {
                if (addFile)
                {
                    // Add a simple file copy
                    // This exists primarily to avoid the mod being treated as always enabled after importing since all of the files are copied
                    await mod.AddFileCopyAsync(new FileCopy("example.txt", "test/example.txt"), new MemoryStream());
                }

                configureOptions?.Invoke(mod);
            }

            var resultStream = new MemoryStream();
            resultStream.Write(modStream.ToArray());
            resultStream.Position = 0;

            return resultStream;
        }
    }
}
