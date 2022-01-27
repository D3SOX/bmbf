using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Services;
using BMBF.Backend.Util;

namespace BMBF.Backend.Implementations;

public class BeatSaverService : IBeatSaverService
{
    private readonly HttpClient _httpClient;
        
    public BeatSaverService(BMBFSettings settings)
    {
        _httpClient = HttpClientUtil.CreateBMBFHttpClient();
        _httpClient.BaseAddress = settings.BeatSaverBaseUri;
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
        var versions = (await JsonDocument.ParseAsync(respStream)).RootElement.GetProperty("versions");

        DateTime latest = DateTime.MinValue;
        string? latestDownloadUri = null;
        foreach (var versionObj in versions.EnumerateArray())
        {
            var hash = versionObj.GetProperty("hash").GetString()?.ToUpper();
            if(hash == null) continue;

            if (preferHash != null && hash != preferHash)
            {
                continue;
            }
            
            var val = versionObj.GetProperty("createdAt").Deserialize<DateTime>();
            if (latest < val)
            {
                latest = val;
                latestDownloadUri = versionObj.GetProperty("downloadURL").GetString();
            }
        }

        // No version found (with correct hash)
        if (latestDownloadUri == null)
        {
            return null;
        }

        var mapResp = await _httpClient.GetAsync(latestDownloadUri, HttpCompletionOption.ResponseHeadersRead);
        mapResp.EnsureSuccessStatusCode();
        return await mapResp.Content.ReadAsStreamAsync();
    }
}