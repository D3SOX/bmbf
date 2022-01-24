using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using MimeTypes;
using Serilog;

namespace BMBF.Backend.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the BMBF middleware and endpoints to an app.
    /// </summary>
    /// <param name="app">App to add BMBF endpoints to</param>
    /// <param name="webRootFileProvider">File provider used for frontend static files</param>
    /// <param name="apiEndpointPrefix">Prefix for API endpoints (i.e. non-frontend routes)</param>
    public static void UseBMBF(this IApplicationBuilder app, IFileProvider webRootFileProvider, string apiEndpointPrefix = "/api")
    {
        app.UseWebSockets();
        
        // Map /api requests to MVC controllers
        app.Map(apiEndpointPrefix, appBuilder =>
        {
            appBuilder.UseRouting();
            appBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        });

        // Add our own middleware for static files
        // We use our own middleware since the default StaticFileMiddleware requires that the file provider can
        // determine the length of its files.
        // Unfortunately, this isn't always the case, for instance, compressed Android assets have no way of finding
        // their length without copying the whole file into memory first, which is inefficient.
        app.Run(async ctx =>
        {
            // Redirect / to /index.html
            if (ctx.Request.Path == "/")
            {
                ctx.Request.Path = "/index.html";
            }

            var file = webRootFileProvider.GetFileInfo(ctx.Request.Path);
            if (!file.Exists)
            {
                ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
                return;
            }
            
            var extension = Path.GetExtension(ctx.Request.Path);
            // Attempt to find the Content-Type for this file extension
            if (MimeTypeMap.TryGetMimeType(extension, out var mimeType))
            {
                ctx.Response.StatusCode = (int) HttpStatusCode.OK;
                ctx.Response.Headers["Content-Type"] = mimeType;
                
                // Copy the static file into our response
                await using var fileStream = file.CreateReadStream();
                await fileStream.CopyToAsync(ctx.Response.Body);
            }
            else
            {
                // Fallback failure if no mime type is available
                Log.Warning($"Could not serve static file {ctx.Request.Path} - could not determine Content-Type for {extension}");
                ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
            }
        });
    }
}