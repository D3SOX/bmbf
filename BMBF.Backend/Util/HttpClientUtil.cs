using System.Net.Http;
using System.Reflection;

namespace BMBF.Backend.Util;

public static class HttpClientUtil
{
    // ReSharper disable once InconsistentNaming
    public static HttpClient CreateBMBFHttpClient()
    {
        var client = new HttpClient();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"BMBF/{version}");
        return client;
    }
}