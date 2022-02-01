using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
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

            QModProvider provider = new QModProvider(PackageId, "/mods", "/libs", httpClient, fileSystem, modManagerMock.Object);

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
        /// <param name="addFile"></param>
        /// <returns></returns>
        public static MemoryStream CreateTestingMod(Action<QuestPatcher.QMod.QMod>? configureOptions = null, bool addFile = true)
        {
            var backingStream = new MemoryStream();
            var stream = new MemoryStream();
            var mod = new QuestPatcher.QMod.QMod(
                stream,
                "test-mod",
                "Test Mod",
                Version.Parse("1.0.0"),
                PackageId,
                "1.0.0",
                "BMBF"
            );
            if (addFile)
            {
                // Add a simple file copy
                // This exists primarily to avoid the mod being treated as always enabled after importing since all of the files are copied
                mod.AddFileCopyAsync(new FileCopy("example.txt", "test/example.txt"), new MemoryStream()).Wait();
            }

            configureOptions?.Invoke(mod);
            mod.Dispose(); // Save the manifest
            stream.Dispose();

            backingStream.Write(stream.ToArray()); // ToArray is safe, even if the MemoryStream is disposed
            backingStream.Position = 0;
            return backingStream;
        }
    }
}