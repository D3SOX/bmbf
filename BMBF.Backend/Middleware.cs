﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace BMBF.Backend;

/// <summary>
/// BMBF middleware which reroutes requests to the API path to go to ASP.NET core MVC.
/// Requests other than those to the API path will be mapped to the asset files in wwwroot
/// </summary>
public class Middleware
{
    private const string ApiPath = "/api";
    private const string AssetsWebRoot = "wwwroot/";
        
    private readonly RequestDelegate _next;
    private readonly IFileProvider _assetProvider;
        
    public Middleware(RequestDelegate next, IFileProvider assetProvider)
    {
        _next = next;
        _assetProvider = assetProvider;
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
            if (context.Request.Path == "/")
            {
                context.Request.Path = "/index.html";
            }

            // Otherwise, we need to return the appropriate file from assets
            string assetFilePath = Path.Combine(AssetsWebRoot, context.Request.Path.Value.Substring(1));
            var assetFile = _assetProvider.GetFileInfo(assetFilePath);
            if (assetFile.Exists)
            {
                await using var assetFileStream = assetFile.CreateReadStream();
                
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                string lastPathSection = context.Request.Path.Value.Split("/").Last();
                // Find the appropriate content type and copy to the response body
                context.Response.ContentType = MimeTypes.MimeTypeMap.GetMimeType(lastPathSection);
                await assetFileStream.CopyToAsync(context.Response.Body);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
    }
}