using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Implementations;
using BMBF.Backend.Models.BeatSaver;
using Moq;
using Moq.Protected;
using Xunit;

namespace BMBF.Backend.Tests;

public class BeatSaverServiceTests
{
    private Uri BaseAddress => new("https://example.com");
    
    /// <summary>
    /// Creates a new instance of <see cref="BeatSaverService"/> with a mock <see cref="HttpClient"/>
    /// </summary>
    /// <returns>The created <see cref="BeatSaverService"/> and <see cref="HttpMessageHandler"/> mock for configuration</returns>
    private (BeatSaverService, Mock<HttpMessageHandler>) Create()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = BaseAddress
        };
        
        return (new BeatSaverService(httpClient), handlerMock);
    }

    /// <summary>
    /// Sets up the given handler to return the <see cref="content"/> when the sub-url <see cref="requestUrl"/>.
    /// <see cref="BaseAddress"/> will be automatically prepended to <see cref="requestUrl"/>
    /// </summary>
    /// <param name="handlerMock">Mock to register to content</param>
    /// <param name="requestUrl">URL for the content</param>
    /// <param name="content">Content to return</param>
    private void SetupContent(Mock<HttpMessageHandler> handlerMock, string requestUrl, HttpContent content)
    {
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri == new Uri(BaseAddress, requestUrl)),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = content
            }));
    }

    /// <summary>
    /// Verifies that the given URL on the <see cref="HttpMessageHandler"/> mock was requested the given number of times.
    /// </summary>
    /// <param name="handlerMock">Mock to verify</param>
    /// <param name="requestUrl">URL, <see cref="BaseAddress"/> will be automatically prepended to this</param>
    /// <param name="times">Number of times <paramref name="requestUrl"/> should have been requested</param>
    private void VerifyRequested(Mock<HttpMessageHandler> handlerMock, string requestUrl, Times times)
    {
        handlerMock.Protected()
            .Verify<Task<HttpResponseMessage>>("SendAsync", times,
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri == new Uri(BaseAddress, requestUrl)),
                ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Sets up the given map URL to return the given map versions
    /// </summary>
    /// <param name="mapUrl"></param>
    /// <param name="handlerMock"></param>
    /// <param name="mapVersions"></param>
    private void SetupVersions(Mock<HttpMessageHandler> handlerMock, string mapUrl, params MapVersion[] mapVersions)
    {
        SetupContent(handlerMock, mapUrl, new StringContent(JsonSerializer.Serialize(new Map(mapVersions.ToList()))));
    }

    [Fact]
    public async Task ShouldChooseVersionWithCorrectHash()
    {
        var (service, handlerMock) = Create();
        SetupVersions(handlerMock, "maps/hash/1",
                // Even though this version is older, it should be used as it matches the correct hash
                new MapVersion("1", new Uri(BaseAddress, "map.zip"), new DateTime(0)),
                new MapVersion("2", new Uri(BaseAddress, "wrongMap.zip"), new DateTime(1)));
        SetupContent(handlerMock, "map.zip", new StringContent("example"));

        var result = await service.DownloadSongByHash("1");
        Assert.NotNull(result);
        VerifyRequested(handlerMock, "map.zip", Times.Once());
    }

    [Fact]
    public async Task ShouldChooseLatestVersion()
    {
        var (service, handlerMock) = Create();

        SetupVersions(handlerMock, "maps/id/1",
            // The newer map should always be downloaded when downloading by key
            new MapVersion("", new Uri(BaseAddress, "wrongMap.zip"), new DateTime(0)),
            new MapVersion("", new Uri(BaseAddress, "map.zip"), new DateTime(1)));
        
        SetupContent(handlerMock, "map.zip", new StringContent("example"));

        var result = await service.DownloadSongByKey("1");
        
        Assert.NotNull(result);
        VerifyRequested(handlerMock, "map.zip", Times.Once());
    }
}