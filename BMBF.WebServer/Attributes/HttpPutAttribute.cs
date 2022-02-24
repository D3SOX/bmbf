using System.Net.Http;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// Annotates a method for receiving HTTP PUT requests
/// </summary>
public class HttpPutAttribute : EndpointAttribute
{
    public HttpPutAttribute(string path) : base(HttpMethod.Put, path)
    {
    }
}
