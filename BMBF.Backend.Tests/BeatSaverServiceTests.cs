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

public class BeatSaverServiceTests : IDisposable
{
    private Uri BaseAddress => new("https://example.com");


    public BeatSaverServiceTests()
    {
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = BaseAddress
        };
        _beatSaverService = new BeatSaverService(_httpClient);
    }

    private readonly Mock<HttpClientHandler> _handlerMock = new();
    private readonly HttpClient _httpClient;
    private readonly BeatSaverService _beatSaverService;

    /// <summary>
    /// Sets up the client to return the <see cref="content"/> when the sub-url <see cref="requestUrl"/>.
    /// </summary>
    /// <param name="requestUrl">URL for the content</param>
    /// <param name="content">Content to return</param>
    private void SetupGet(string requestUrl, HttpContent content)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get &&
                                                                                m.RequestUri == new Uri(BaseAddress, requestUrl)),
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
    /// <param name="requestUrl">URL, <see cref="BaseAddress"/> will be automatically prepended to this</param>
    /// <param name="times">Number of times <paramref name="requestUrl"/> should have been requested</param>
    private void VerifyGet(string requestUrl, Times times)
    {
        _handlerMock.Protected()
            .Verify<Task<HttpResponseMessage>>("SendAsync", times,
                ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get && m.RequestUri == new Uri(BaseAddress, requestUrl)),
                ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Sets up the given map URL to return the given map versions
    /// </summary>
    /// <param name="mapUrl">URL of the map to setup versions for</param>
    /// <param name="mapVersions">Map versions to return</param>
    private void SetupGetVersions(string mapUrl, params MapVersion[] mapVersions)
    {
        SetupGet(mapUrl, new StringContent(JsonSerializer.Serialize(new Map(mapVersions.ToList()))));
    }

    [Fact]
    public async Task ShouldChooseVersionWithCorrectHash()
    {
        SetupGetVersions("maps/hash/1",
                // Even though this version is older, it should be used as it matches the correct hash
                new MapVersion("1", new Uri(BaseAddress, "map.zip"), new DateTime(0)),
                new MapVersion("2", new Uri(BaseAddress, "wrongMap.zip"), new DateTime(1)));
        SetupGet("map.zip", new StringContent("example"));

        var result = await _beatSaverService.DownloadSongByHash("1");
        Assert.NotNull(result);
        VerifyGet("map.zip", Times.Once());
    }

    [Fact]
    public async Task ShouldChooseLatestVersion()
    {
        SetupGetVersions("maps/id/1",
            // The newer map should always be downloaded when downloading by key
            new MapVersion("", new Uri(BaseAddress, "wrongMap.zip"), new DateTime(0)),
            new MapVersion("", new Uri(BaseAddress, "map.zip"), new DateTime(1)));

        SetupGet("map.zip", new StringContent("example"));

        var result = await _beatSaverService.DownloadSongByKey("1");

        Assert.NotNull(result);
        VerifyGet("map.zip", Times.Once());
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}