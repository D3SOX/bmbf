using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace BMBF.WebServer
{
    public delegate Task<Response> Handler(Request request);
    public delegate Task<Response> Middleware(Request request, Handler next);

    public class Router
    {
        internal readonly List<Route> routes = new();
        private readonly List<Middleware> middlewares = new();

        public void Route(HttpMethod method, string path, Handler handler)
        {
            foreach (var middleware in middlewares) handler = handler.With(middleware);
            routes.Add(new Route(method, new Path(path), handler));
        }

        public void Use(Middleware middleware)
        {
            middlewares.Add(middleware);
            foreach (var route in routes) route.Use(middleware);
        }

        public void Mount(string path, Router other)
        {
            var truePath = new Path(path);
            foreach (var route in other.routes)
            {
                var newRoute = route.Mounted(truePath);
                foreach (var middleware in middlewares) newRoute.Use(middleware);
                routes.Add(route);
            }
        }

        public void Get(string path, Handler handler) => Route(HttpMethod.Get, path, handler);
        public void Post(string path, Handler handler) => Route(HttpMethod.Post, path, handler);
        public void Put(string path, Handler handler) => Route(HttpMethod.Put, path, handler);
        public void Delete(string path, Handler handler) => Route(HttpMethod.Delete, path, handler);
    }

    internal class Route
    {
        public HttpMethod Method;
        public Path Path;
        public Handler Handler;

        public Route(HttpMethod method, Path path, Handler handler)
        {
            Method = method;
            Path = path;
            Handler = handler;
        }

        public bool Matches(Request request)
        {
            if (MethodMatches(request.Method) && Path.Matches(request.Url.AbsolutePath, out var extracted))
            {
                request.AddParams(extracted);
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool MethodMatches(HttpMethod method) => Method == method || (Method == HttpMethod.Get && method == HttpMethod.Head);

        public void Use(Middleware middleware)
        {
            Handler = Handler.With(middleware);
        }

        public Route Mounted(Path path) => new(Method, path.Join(Path), Handler);
    }

    public static class RoutingExt
    {
        public static Handler With(this Handler handler, Middleware middleware) => (Request request) => middleware(request, handler);
    }
}
