using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Web;

namespace BMBF.WebServer
{
    public class Request
    {
        public HttpRequest Inner { get; }
        public IPEndPoint Peer { get; }
        public HttpMethod Method { get; }
        public string Path { get; }

        private readonly HeaderDictionary _headers = new();
        private readonly Dictionary<string, string> _queryParameters = new();
        private readonly Dictionary<string, string> _parameters = new();

        internal Request(HttpRequest inner, IPEndPoint peer)
        {
            Inner = inner;
            Peer = peer;
            Method = new HttpMethod(inner.Method);

            // Create a temporary URI for parsing purposes
            var uri = new Uri($"http://127.0.0.1{inner.Url}");
            Path = uri.AbsolutePath;

            for (int i = 0; i < inner.Headers; i++)
            {
                var (key, value) = inner.Header(i);
                _headers.Add(key, value);
            }

            try
            {
                var query = HttpUtility.ParseQueryString(uri.Query);
                for (int i = 0; i < query.Count; i++)
                {
                    _queryParameters.Add(query.GetKey(i)!, query.Get(i)!);
                }
            }
            catch
            {
                throw new WebException(Response.Text("Invalid query string", 400));
            }
        }

        public IDictionary<string, string> Headers => _headers;
        public IDictionary<string, string> Parameters => _parameters;
        public IDictionary<string, string> QueryParameters => _queryParameters;

        public Span<byte> Body => Inner.BodySpan;

        public string TextBody()
        {
            string? s;
            var ex = new WebException(Response.Text("Invalid UTF8 request body", 400));

            try
            {
                s = Encoding.UTF8.GetString(Body);
            }
            catch
            {
                throw ex;
            }

            return s ?? throw ex;
        }
        public T JsonBody<T>()
        {
            T? json;

            try
            {
                json = JsonSerializer.Deserialize<T>(Body);
            }
            catch (JsonException ex)
            {
                throw new WebException(Response.Text(ex.Message, 400));
            }

            return json is not null ? json : throw new WebException(Response.Text("Invalid JSON request body", 400));
        }

        public T Param<T>(string name) where T : IConvertible
        {
            object? value;
            var ex = new WebException(Response.Text($"Invalid path parameter '{name}'", 400));

            try
            {
                value = Convert.ChangeType(_parameters[name], typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                throw ex;
            }

            return value is not null ? (T) value : throw ex;
        }
        internal void AddParams(IDictionary<string, string> parameters)
        {
            foreach (var (name, value) in parameters)
            {
                _parameters.Add(name, value);
            }
        }

        public T QueryParam<T>(string name) where T : IConvertible
        {
            object? value;
            var ex = new WebException(Response.Text($"Invalid query parameter '{name}'", 400));

            try
            {
                value = Convert.ChangeType(_queryParameters[name], typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                throw ex;
            }

            return value is not null ? (T) value : throw ex;
        }
    }
}
