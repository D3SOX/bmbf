using System.Net.Http;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// Annotates a method for receiving HTTP PATCH requests
/// </summary>
public class HttpPatchAttribute : EndpointAttribute
{
    public HttpPatchAttribute(string path) : base(HttpMethod.Patch, path)
    {
    }
}
