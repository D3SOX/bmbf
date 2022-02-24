using System;

namespace BMBF.WebServer;

public class EndpointExceptionEventArgs
{
    internal EndpointExceptionEventArgs(Exception exception, string requestPath)
    {
        Exception = exception;
        RequestPath = requestPath;
    }

    /// <summary>
    /// Exception thrown by the endpoint
    /// </summary>
    public Exception Exception { get; }
    
    /// <summary>
    /// Path of the request that failed to be handled
    /// </summary>
    public string RequestPath { get; }
}
