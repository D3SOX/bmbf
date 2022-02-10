using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.WebServer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MimeMapping;
using Serilog;

namespace BMBF.Backend.Implementations;

public class WebService : IHostedService, IDisposable
{
    private readonly Server _server;
    private readonly BMBFSettings _settings;
    private readonly IFileProvider _webRootFileProvider;

    public WebService(BMBFSettings settings, FileProviders fileProviders)
    {
        _settings = settings;
        _server = new Server(settings.BindAddress, settings.BindPort);
        _webRootFileProvider = fileProviders.WebRootProvider;
        
        var apiRouter = new Router();
        SetupApi(apiRouter);
        
        // First add our API endpoints
        _server.Mount("/api", apiRouter);
        
        // Make sure that / points to /index.html
        _server.Get("/", req =>
        {
            req.Path = "/index.html";
            return StaticFileHandler(req);
        });
        
        // Route remaining requests to static files
        _server.Get("*", StaticFileHandler);
    }

    protected virtual void SetupApi(Router router)
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        
        router.Get("/version", _ => Response.Text($"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}").Async());
    }

    private async Task<Response> StaticFileHandler(Request req)
    {
        var file = _webRootFileProvider.GetFileInfo(req.Path);
        if (!file.Exists)
        {
            return Response.Text("Not found", 404);
        }

        await using var readStream = file.CreateReadStream();
        using var bodyStream = new MemoryStream();
        await readStream.CopyToAsync(bodyStream);

        return new Response(bodyStream.ToArray(), 200, MimeUtility.GetMimeMapping(file.Name));
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information($"Web server starting up on {_settings.BindAddress}:{_settings.BindPort}");
        _server.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping web server");
        _server.Stop();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}
