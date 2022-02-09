using NetCoreServer;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BMBF.WebServer
{
    public class Server : Router
    {
        private readonly InternalServer server;

        public Server(IPEndPoint endpoint)
        {
            server = new InternalServer(this, endpoint);
        }
        public Server(IPAddress address, int port)
        {
            server = new InternalServer(this, address, port);
        }
        public Server(string address, int port)
        {
            server = new InternalServer(this, address, port);
        }

        public virtual bool Start() => server.Start();
        public virtual bool Stop() => server.Stop();
        public virtual bool Restart() => server.Restart();

        public delegate void ErrorHandler(object sender, SocketError error);
        public event ErrorHandler? Error;
        public virtual void OnError(SocketError e) => Error?.Invoke(this, e);

        public delegate void ExceptionHandler(object sender, Exception exception);
        public event ExceptionHandler? Exception;
        public virtual void OnException(Exception e) => Exception?.Invoke(this, e);
    }

    internal class InternalServer : HttpServer
    {
        private readonly Server server;

        public InternalServer(Server server, IPEndPoint endpoint) : base(endpoint)
        {
            this.server = server;
        }
        public InternalServer(Server server, IPAddress address, int port) : base(address, port)
        {
            this.server = server;
        }
        public InternalServer(Server server, string address, int port) : base(address, port)
        {
            this.server = server;
        }

        protected override InternalSession CreateSession()
        {
            return new InternalSession(server, this);
        }

        protected override void OnError(SocketError error) => server.OnError(error);
    }

    internal class InternalSession : HttpSession
    {
        private readonly Server server;

        public InternalSession(Server server, InternalServer internalServer) : base(internalServer)
        {
            this.server = server;
        }

        protected override void OnReceivedRequest(HttpRequest innerRequest)
        {
            var request = new Request(innerRequest, (IPEndPoint) Socket.RemoteEndPoint!);
            var handler = server.routes.Find((route) => route.Matches(request))?.Handler;

            if (handler is null)
            {
                Response response;

                if (request.Method == HttpMethod.Options)
                {
                    var allowed = server.routes
                        .Where((route) => route.Path.Matches(request.Url.AbsolutePath, out var _extracted))
                        .Select((route) => route.Method)
                        .Distinct();
                    response = WebServer.Response.Text("", 204);
                    foreach (var method in allowed) response.Headers.Add("Allow", method.ToString());
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
                handler(request).ContinueWith((task) =>
                {
                    var response = task.Status switch
                    {
                        TaskStatus.RanToCompletion => task.Result!,
                        TaskStatus.Faulted when task.Exception!.GetBaseException() is WebException ex => ex.Response,
                        TaskStatus.Faulted => OnException(task.Exception!),
                        _ => WebServer.Response.Text("Internal Server Error", 500),
                    };
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
            server.OnException(ex);
            return WebServer.Response.Text("Internal Server Error", 500);
        }

        protected override void OnError(SocketError error) => server.OnError(error);
    }
}
