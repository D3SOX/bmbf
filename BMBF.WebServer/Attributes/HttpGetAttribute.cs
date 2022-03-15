using System.Net.Http;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// Annotates a method for receiving HTTP GET requests
/// </summary>
public class HttpGetAttribute : EndpointAttribute
{
    public HttpGetAttribute(string path) : base(HttpMethod.Get, path)
    {
    }
}
