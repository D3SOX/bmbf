using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BMBF.Desktop;

public class Middleware
{
    private const string ApiPath = "/api";
        
    private readonly RequestDelegate _next;
        
    public Middleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context == null)
        {
            throw new NullReferenceException(nameof(context));
        }
        
        // If this is an API request, forward it to ASP.NET core MVC
        if (context.Request.Path.StartsWithSegments(ApiPath, out var matchedPath, out var remainingPath))
        {
            var originalPath = context.Request.Path;
            var originalPathBase = context.Request.PathBase;
            context.Request.Path = remainingPath;
            context.Request.PathBase = originalPathBase.Add(matchedPath);

            try
            {
                await _next(context);
            }
            finally
            {
                context.Request.Path = originalPath;
                context.Request.PathBase = originalPathBase;
            }
        }
        else
        {
            context.Response.StatusCode = (int) HttpStatusCode.NotFound;
            await context.Response.WriteAsync("Currently, the BMBF desktop wrapper does not support serving the frontend." +
                                              "\nIt is expected that when using the desktop wrapper, you have the frontend served locally (i.e. with npm run start) instead of hosting through BMBF.");
        }
    }
}