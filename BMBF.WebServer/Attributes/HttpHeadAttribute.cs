using System.Net.Http;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// Annotates a method for receiving HTTP HEAD requests
/// </summary>
public class HttpHeadAttribute : EndpointAttribute
{
    public HttpHeadAttribute(string path) : base(HttpMethod.Head, path)
    {
    }
}
