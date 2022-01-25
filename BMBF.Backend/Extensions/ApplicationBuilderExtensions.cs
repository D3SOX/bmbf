using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MimeTypes;
using Serilog;

namespace BMBF.Backend.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the BMBF middleware and endpoints to an app.
    /// </summary>
    /// <param name="app">App to add BMBF endpoints to</param>
    /// <param name="ctx">Context of the web app</param>
    /// <param name="webRootFileProvider">File provider used for frontend static files</param>
    /// <param name="apiEndpointPrefix">Prefix for API endpoints (i.e. non-frontend routes)</param>
    public static void UseBMBF(this IApplicationBuilder app, WebHostBuilderContext ctx, IFileProvider webRootFileProvider, string apiEndpointPrefix = "/api")
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

            if (ctx.HostingEnvironment.IsDevelopment())
            {
                appBuilder.UseSwagger();
                appBuilder.UseSwaggerUI();
            }
        });

        // Add our own middleware for static files
        // We use our own middleware since the default StaticFileMiddleware requires that the file provider can
        // determine the length of its files.
        // Unfortunately, this isn't always the case, for instance, compressed Android assets have no way of finding
        // their length without copying the whole file into memory first, which is inefficient.
        app.Run(async reqCtx =>
        {
            // Redirect / to /index.html
            if (reqCtx.Request.Path == "/")
            {
                reqCtx.Request.Path = "/index.html";
            }

            var file = webRootFileProvider.GetFileInfo(reqCtx.Request.Path);
            if (!file.Exists)
            {
                reqCtx.Response.StatusCode = (int) HttpStatusCode.NotFound;
                return;
            }
            
            var extension = Path.GetExtension(reqCtx.Request.Path);
            // Attempt to find the Content-Type for this file extension
            if (MimeTypeMap.TryGetMimeType(extension, out var mimeType))
            {
                reqCtx.Response.StatusCode = (int) HttpStatusCode.OK;
                reqCtx.Response.Headers["Content-Type"] = mimeType;
                
                // Copy the static file into our response
                await using var fileStream = file.CreateReadStream();
                await fileStream.CopyToAsync(reqCtx.Response.Body);
            }
            else
            {
                // Fallback failure if no mime type is available
                Log.Warning($"Could not serve static file {reqCtx.Request.Path} - could not determine Content-Type for {extension}");
                reqCtx.Response.StatusCode = (int) HttpStatusCode.NotFound;
            }
        });
    }
}