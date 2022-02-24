using System.Net.Http;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// Annotates a method for receiving HTTP DELETE requests
/// </summary>
public class HttpDeleteAttribute : EndpointAttribute
{
    public HttpDeleteAttribute(string path) : base(HttpMethod.Delete, path)
    {
    }
}
