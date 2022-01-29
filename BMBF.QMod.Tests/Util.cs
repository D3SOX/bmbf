using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BMBF.ModManagement;
using Moq;
using Moq.Protected;
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
        /// <param name="httpClient">If not null, this will be used to override the default HttpClient</param>
        /// <param name="fileSystem">If not null, this will be used to override the default mock FileSystem</param>
        /// <returns></returns>
        public static QModProvider CreateProvider(HttpClient? httpClient = null, IFileSystem? fileSystem = null)
        {
            var modManagerMock = new Mock<IModManager>();

            QModProvider provider = new QModProvider(PackageId, "/mods", "/libs", httpClient ?? new HttpClient(), fileSystem ?? new MockFileSystem(), modManagerMock.Object);
         
            var installLock = new SemaphoreSlim(1);
            modManagerMock.SetupGet(m => m.InstallLock).Returns(installLock);
            modManagerMock.Setup(m => m.ImportMod(provider, It.IsAny<Stream>(), It.IsAny<string>()))
                .Returns(async delegate(IModProvider genericProvider, Stream stream, string _)
                {
                    var modProvider = (QModProvider)genericProvider;
                    
                    var mod = await modProvider.TryParseModAsync(stream) ?? throw new NullReferenceException();
                    await provider.AddModAsyncInternal((QMod) mod, new HashSet<string>());
                    return (QMod) mod;
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
                mod.AddFileCopyAsync(new FileCopy("example.txt", "test/example.txt"), new MemoryStream());
            }

            configureOptions?.Invoke(mod);
            mod.Dispose(); // Save the manifest
            stream.Dispose();
            
            backingStream.WriteAsync(stream.ToArray()); // ToArray is safe, even if the MemoryStream is disposed
            backingStream.Position = 0;
            return backingStream;
        }

        /// <summary>
        /// Creates an <see cref="HttpClient"/> that will always respond to a URL with the given <see cref="Stream"/>
        /// </summary>
        /// <param name="response">Content stream to respond with</param>
        /// <param name="requestUrl">URL of requests to return this response for</param>
        /// <returns>An <see cref="HttpClient"/> that will respond to the request with the given stream as content</returns>
        public static HttpClient CreateStreamHttpClientMock(string requestUrl, Stream response)
        {
            return CreateHttpClientMock(requestUrl, new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(response)
            });
        }
        
        /// <summary>
        /// Creates an <see cref="HttpClient"/> that will always respond to the given URL with a 404 response code.
        /// </summary>
        /// <param name="requestUrl">URL of requests to return 404 for</param>
        /// <returns>An <see cref="HttpClient"/> that will respond to any request to ths given URL with a 404</returns>
        public static HttpClient CreateNotFoundHttpClientMock(string requestUrl)
        {
            return CreateHttpClientMock(requestUrl, new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });
        }

        private static HttpClient CreateHttpClientMock(string requestUrl, HttpResponseMessage responseMessage)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri == new Uri(requestUrl)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(responseMessage));

            return new HttpClient(mock.Object);
        }
        
    }
}