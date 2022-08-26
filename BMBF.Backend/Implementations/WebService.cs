using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMBF.Backend.Configuration;
using BMBF.Backend.Endpoints;
using BMBF.Backend.Services;
using BMBF.WebServer;
using Hydra;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MimeMapping;
using Serilog;
using Server = BMBF.WebServer.Server;

namespace BMBF.Backend.Implementations;


public class WebService : IHostedService, IDisposable
{
    private readonly Server _server;
    private readonly BMBFSettings _settings;
    private readonly IFileProvider _webRootFileProvider;
    private readonly CancellationTokenSource _cts = new();
    private Task? _webServerTask;

    public WebService(BMBFSettings settings,
        FileProviders fileProviders,
        JsonSerializerOptions serializerOptions,
        IEnumerable<IEndpoints> endpoints,
        AuthEndpoints authEndpoints,
        IAuthService authService)
    {
        _settings = settings;
        _webRootFileProvider = fileProviders.WebRootProvider;
        _server = new Server(settings.BindAddress, settings.BindPort);

        _server.ServerException += OnException;
        _server.EndpointException += OnEndpointException;

        Responses.DefaultSerializerOptions = serializerOptions;

        var apiRouter = new Router();
        // Add all configured implementations of IEndpoints
        foreach (var endpointObject in endpoints)
        {
            apiRouter.AddEndpoints(endpointObject);
        }

        var authRouter = new Router();
        authRouter.AddEndpoints(authEndpoints);
        // This is crucial, never remove this line - it would allow anybody to add arbitrary authentication credentials.
        authRouter.Use(RequireLoopback);

        // First add our API endpoints
        _server.Mount("/api", apiRouter);
        _server.Mount("/auth", authRouter);

        // Make sure that / points to /index.html
        _server.Get("/", req =>
        {
            req.Path = "/index.html";
            return Task.FromResult(StaticFileHandler(req));
        });

        // Route remaining requests to static files
        _server.Get("*", req => Task.FromResult(StaticFileHandler(req)));

        _server.Use(authService.Authenticate);
        _server.Use(async (req, next) =>
        {
            var resp = await next(req);

            resp.Headers["Access-Control-Allow-Origin"] = "*";
            return resp;
        });
    }

    private async Task<HttpResponse> RequireLoopback(Request request, Handler next)
    {
        if (request.Inner.Remote is IPEndPoint endPoint && IPAddress.IsLoopback(endPoint.Address))
        {
            return await next(request);
        }
        return Responses.Text("Only loopback clients are allowed to access this endpoint", 403);
    }

    private void OnException(object? sender, Exception ex)
    {
        Log.Error(ex, "Exception occured while handling HTTP request");
    }

    private void OnEndpointException(object? sender, EndpointExceptionEventArgs args)
    {
        Log.Error(args.Exception, $"Exception occured handling request to {args.RequestPath}");
    }

    private HttpResponse StaticFileHandler(Request req)
    {
        var file = _webRootFileProvider.GetFileInfo(req.Path);
        if (!file.Exists)
        {
            if (Path.GetExtension(req.Path).Length == 0)
            {
                file = _webRootFileProvider.GetFileInfo("/index.html");
            }
            else
            {
                return Responses.NotFound();
            }
        }

        var readStream = file.CreateReadStream();
        return new HttpResponse(200, readStream)
        {
            Headers =
            {
                ["Content-Type"] = MimeUtility.GetMimeMapping(file.Name),
                ["Connection"] = "Close"
            }
        };
    }

    private async Task ServerHandler()
    {
        try
        {
            Log.Information($"Web server starting up on {_settings.BindAddress}:{_settings.BindPort}");
            await _server.Run(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Server shut down
            Log.Information("Web server shutdown complete");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while starting the web server");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _webServerTask = ServerHandler();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping web server");

        _cts.Cancel();
        if (_webServerTask != null)
        {
            await Task.WhenAny(Task.Delay(2000, cancellationToken), _webServerTask);
        }
    }

    public void Dispose()
    {
        _server.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}
