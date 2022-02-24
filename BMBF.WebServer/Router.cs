using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Hydra;

namespace BMBF.WebServer
{
    public delegate Task<HttpResponse> Handler(Request request);
    
    public delegate Task<HttpResponse> Middleware(Request request, Handler next);

    public class Router
    {
        internal List<Route> Routes { get; } = new();
        private readonly List<Middleware> _middlewares = new();

        public void Route(HttpMethod method, string path, Handler handler)
        {
            foreach (var middleware in _middlewares) handler = handler.With(middleware);
            Routes.Add(new Route(method, new Path(path), handler));
        }

        public void Use(Middleware middleware)
        {
            _middlewares.Add(middleware);
            foreach (var route in Routes)
            {
                route.Use(middleware);
            }
        }

        public void Mount(string path, Router other)
        {
            var truePath = new Path(path);
            foreach (var route in other.Routes)
            {
                var newRoute = route.Mounted(truePath);
                foreach (var middleware in _middlewares)
                {
                    newRoute.Use(middleware);
                }

                Routes.Add(newRoute);
            }
        }

        public void Get(string path, Handler handler) => Route(HttpMethod.Get, path, handler);
        public void Post(string path, Handler handler) => Route(HttpMethod.Post, path, handler);
        public void Put(string path, Handler handler) => Route(HttpMethod.Put, path, handler);
        public void Delete(string path, Handler handler) => Route(HttpMethod.Delete, path, handler);
    }

    internal class Route
    {
        internal HttpMethod Method { get; }
        internal Path Path { get; }
        internal Handler Handler { get; private set; }

        internal Route(HttpMethod method, Path path, Handler handler)
        {
            Method = method;
            Path = path;
            Handler = handler;
        }

        internal bool Matches(Request request)
        {
            if (MethodMatches(request.Method) && Path.Matches(request.Path, out var extracted))
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

        internal void Use(Middleware middleware)
        {
            Handler = Handler.With(middleware);
        }

        internal Route Mounted(Path path) => new(Method, path.Join(Path), Handler);
    }

    public static class RoutingExt
    {
        public static Handler With(this Handler handler, Middleware middleware) => request => middleware(request, handler);
    }
}
