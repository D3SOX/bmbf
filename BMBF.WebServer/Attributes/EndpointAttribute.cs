using System;
using System.Net.Http;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// An attribute which annotates a method as an HTTP endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EndpointAttribute : Attribute
{
    /// <summary>
    /// HTTP method used to access this endpoint
    /// </summary>
    internal HttpMethod Method { get; }

    /// <summary>
    /// Request path for this endpoint, possibly including template parameters
    /// </summary>
    internal string Path { get; }

    internal EndpointAttribute(HttpMethod method, string path)
    {
        Method = method;
        Path = path;
    }
}
