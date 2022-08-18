using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hydra;

namespace BMBF.WebServer;

public class Server : Router, IDisposable
{
    private readonly Hydra.Server _server;
    
    public Server(string bindAddress, int bindPort)
    {
        _server = new Hydra.Server(
            new IPEndPoint(IPAddress.Parse(bindAddress), bindPort),
            RequestHandler
        );
        _server.Exception += (_, args) => ServerException?.Invoke(this, args.Exception);
    }

    public virtual async Task Run(CancellationToken ct) => await _server.Run(ct);
    public event EventHandler<EndpointExceptionEventArgs>? EndpointException;
    public event EventHandler<Exception>? ServerException;

    private async Task<HttpResponse> RequestHandler(HttpRequest innerRequest)
    {
        var request = new Request(innerRequest);
        
        var route = Routes.Find(route => route.Matches(request));
        if (route is null)
        {
            if (request.ParsedMethod == HttpMethod.Options)
            {
                var allowed = Routes
                    .Where(r => r.Path.Matches(request.Path, out _))
                    .Select(r => r.Method)
                    .Distinct();
                var response = Responses.Empty(204);
                foreach (var method in allowed)
                {
                    response.Headers.Add("Allow", method.ToString());
                    // TODO: Make this configurable?
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                return response;
            }
            else
            {
                return Responses.NotFound();
            }
        }

        try
        {
            await innerRequest.ReadHeaders();
            var response = await route.Handler(request);
            if (request.ParsedMethod == HttpMethod.Head && route.Method != HttpMethod.Head)
            {
                response.Headers.Add("Content-Length", response.Body.Length.ToString());
                response.Body = Stream.Null;
            }

            return response;
        }
        catch (WebException ex)
        {
            return ex.Response;
        }
        catch (Exception ex)
        {
            EndpointException?.Invoke(this, new EndpointExceptionEventArgs(ex, request.Path));
            return Responses.InternalServerError();
        }
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}
