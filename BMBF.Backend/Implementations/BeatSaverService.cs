using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Services;
using BMBF.Backend.Util;
using Newtonsoft.Json.Linq;

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
        return await FindAndDownload($"maps/hash/{hash}");
    }

    public async Task<Stream?> DownloadSongByKey(string key)
    {
        return await FindAndDownload($"maps/id/{key}");
    }

    private async Task<Stream?> FindAndDownload(string mapInfoUri)
    {
        using var resp = await _httpClient.GetAsync(mapInfoUri, HttpCompletionOption.ResponseHeadersRead);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
            
        string respString = await resp.Content.ReadAsStringAsync();

        var versions = JToken.Parse(respString).Value<JArray>("versions");
        if (versions == null)
        {
            throw new FormatException("Map had no versions property");
        }
            
        DateTime latest = DateTime.MinValue;
        string? latestDownloadUri = null;
        foreach (var versionObj in versions)
        {
            var val = versionObj.Value<DateTime>("createdAt");
            if (latest < val)
            {
                latest = val;
                latestDownloadUri = versionObj.Value<string>("downloadURL");
            }
        }
            
        var mapResp = await _httpClient.GetAsync(latestDownloadUri, HttpCompletionOption.ResponseHeadersRead);
        mapResp.EnsureSuccessStatusCode();
        return await mapResp.Content.ReadAsStreamAsync();
    }
}