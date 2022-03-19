using System;
using System.IO;
using System.Linq;
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

    private async Task<Stream?> FindAndDownload(string mapInfoUri, string? withHash)
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
        if (withHash is not null)
        {
            latest = map.Versions.FirstOrDefault(v => v.Hash.Equals(withHash, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            foreach (var version in map.Versions)
            {
                if (latest == null || latest.CreatedAt < version.CreatedAt)
                {
                    latest = version;
                }
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
