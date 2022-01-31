using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BMBF.DiffGenerator;

public class AppVersionFinder
{
    private readonly string _accessToken;
    private readonly HttpClient _httpClient = new();

    public AppVersionFinder(string accessToken, string requestUrl = "https://graph.oculus.com/graphql")
    {
        _httpClient.BaseAddress = new Uri(requestUrl);
        _accessToken = accessToken;
    }

    public async Task<List<OculusAppVersion>> GetAppVersions(long appId)
    {
        var dict = new Dictionary<string, string>();
        dict["access_token"] = _accessToken;
        dict["doc_id"] = "1586217024733717";
        dict["variables"] = $"{{\"id\":\"{appId}\"}}";

        using var resp = await _httpClient.PostAsync("", new FormUrlEncodedContent(dict));
        resp.EnsureSuccessStatusCode();

        await using var contentStream = resp.Content.ReadAsStream();
        var document = await JsonDocument.ParseAsync(contentStream);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return document.RootElement.GetProperty("data")
            .GetProperty("node")
            .GetProperty("supportedBinaries")
            .GetProperty("edges").EnumerateArray()
            .Select(element => element.GetProperty("node").Deserialize<OculusAppVersion>(options)!).ToList();
    }


}