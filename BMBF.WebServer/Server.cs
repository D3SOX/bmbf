using NetCoreServer;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BMBF.WebServer
{
    public class Server : Router, IDisposable
    {
        private readonly InternalServer _server;

        public Server(IPEndPoint endpoint)
        {
            _server = new InternalServer(this, endpoint);
        }
        public Server(IPAddress address, int port)
        {
            _server = new InternalServer(this, address, port);
        }
        public Server(string address, int port)
        {
            _server = new InternalServer(this, address, port);
        }

        public virtual bool Start() => _server.Start();
        public virtual bool Stop() => _server.Stop();
        public virtual bool Restart() => _server.Restart();

        public delegate void ErrorHandler(object sender, SocketError error);
        public event ErrorHandler? Error;
        internal virtual void OnError(SocketError e) => Error?.Invoke(this, e);

        public delegate void ExceptionHandler(object sender, Exception exception);
        public event ExceptionHandler? Exception;
        internal virtual void OnException(Exception e) => Exception?.Invoke(this, e);

        public void Dispose()
        {
            _server.Dispose();
        }
    }

    internal class InternalServer : HttpServer
    {
        private readonly Server _server;

        public InternalServer(Server server, IPEndPoint endpoint) : base(endpoint)
        {
            _server = server;
        }
        public InternalServer(Server server, IPAddress address, int port) : base(address, port)
        {
            _server = server;
        }
        public InternalServer(Server server, string address, int port) : base(address, port)
        {
            _server = server;
        }

        protected override InternalSession CreateSession()
        {
            return new InternalSession(_server, this);
        }

        protected override void OnError(SocketError error) => _server.OnError(error);
    }

    internal class InternalSession : HttpSession
    {
        private readonly Server _server;

        public InternalSession(Server server, InternalServer internalServer) : base(internalServer)
        {
            _server = server;
        }

        protected override void OnReceivedRequest(HttpRequest innerRequest)
        {
            var request = new Request(innerRequest, (IPEndPoint) Socket.RemoteEndPoint!);
            var route = _server.Routes.Find(route => route.Matches(request));

            if (route is null)
            {
                Response response;

                if (request.Method == HttpMethod.Options)
                {
                    var allowed = _server.Routes
                        .Where(r => r.Path.Matches(request.Path, out _))
                        .Select(r => r.Method)
                        .Distinct();
                    response = WebServer.Response.Empty();
                    foreach (var method in allowed)
                    {
                        response.Headers.Add("Allow", method.ToString());
                    }
                }
                else
                {
                    response = WebServer.Response.Text("Not Found", 404);
                }

                SendResponseAsync(response.ToInner(Response));
                return;
            }

            try
            {
                route.Handler(request).ContinueWith(task =>
                {
                    var response = task.Status switch
                    {
                        TaskStatus.RanToCompletion => task.Result,
                        TaskStatus.Faulted when task.Exception!.GetBaseException() is WebException ex => ex.Response,
                        TaskStatus.Faulted => OnException(task.Exception!),
                        _ => WebServer.Response.Text("Internal Server Error", 500),
                    };

                    if (request.Method == HttpMethod.Head && route.Method != HttpMethod.Head)
                    {
                        response.Headers.Add("Content-Length", response.Body.Length.ToString());
                        response.Body = Array.Empty<byte>();
                    }

                    SendResponseAsync(response.ToInner(Response));
                });
            }
            catch (WebException ex)
            {
                SendResponseAsync(ex.Response.ToInner(Response));
            }
            catch (Exception ex)
            {
                SendResponseAsync(OnException(ex).ToInner(Response));
            }
        }

        private Response OnException(Exception ex)
        {
            _server.OnException(ex);
            return WebServer.Response.Text("Internal Server Error", 500);
        }

        protected override void OnError(SocketError error) => _server.OnError(error);
    }
}
