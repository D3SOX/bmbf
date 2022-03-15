using System;
using System.Reflection;

namespace BMBF.WebServer.Attributes;

/// <summary>
/// Thrown if an endpoint does not match the required method signature.
/// </summary>
public class InvalidEndpointException : Exception
{
    /// <summary>
    /// Endpoint method triggering this error
    /// </summary>
    public MethodInfo Method { get; }

    internal InvalidEndpointException(MethodInfo method, string message) : base(message)
    {
        Method = method;
    }
}
