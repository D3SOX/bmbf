using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BMBF.Backend.Models.BeatSaver;
using BMBF.Backend.Services;

namespace BMBF.Backend.Implementations;

public class BeatSaverService : IBeatSaverService
{
    private readonly HttpClient _httpClient;

    public BeatSaverService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Stream?> DownloadSongByHash(string hash)
    {
        return await FindAndDownload($"maps/hash/{hash}", hash);
    }

    public async Task<Stream?> DownloadSongByKey(string key)
    {
        return await FindAndDownload($"maps/id/{key}", null);
    }

    private async Task<Stream?> FindAndDownload(string mapInfoUri, string? preferHash)
    {
        using var resp = await _httpClient.GetAsync(mapInfoUri, HttpCompletionOption.ResponseHeadersRead);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await using var respStream = await resp.Content.ReadAsStreamAsync();
        var map = await JsonSerializer.DeserializeAsync<Map>(respStream);
        if (map == null)
        {
            return null;
        }

        MapVersion? latest = null;
        foreach (var version in map.Versions)
        {
            var hash = version.Hash;

            if (preferHash != null && hash != preferHash)
            {
                continue;
            }

            if (latest == null || latest.CreatedAt < version.CreatedAt)
            {
                latest = version;
            }
        }

        // No version found (with correct hash)
        if (latest == null)
        {
            return null;
        }

        var mapResp = await _httpClient.GetAsync(latest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        mapResp.EnsureSuccessStatusCode();
        return await mapResp.Content.ReadAsStreamAsync();
    }
}