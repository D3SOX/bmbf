using System.Net.Http;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// Annotates a method for receiving HTTP POST requests
/// </summary>
public class HttpPostAttribute : EndpointAttribute
{
    public HttpPostAttribute(string path) : base(HttpMethod.Post, path)
    {
    }
}
