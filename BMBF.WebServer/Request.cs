using NetCoreServer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace BMBF.WebServer
{
    public class Request
    {
        public readonly HttpRequest Inner;
        public readonly IPEndPoint Peer;
        public readonly HttpMethod Method;
        public readonly Uri Url;
        public readonly HttpRequestHeaders Headers;

        private readonly Dictionary<string, string> queryParameters = new();
        private readonly Dictionary<string, string> parameters = new();

        internal Request(HttpRequest inner, IPEndPoint peer)
        {
            Inner = inner;
            Peer = peer;
            Method = new(inner.Method);
            Url = new(inner.Url);

            using (var tmp = new HttpRequestMessage())
            {
                Headers = tmp.Headers;
                Headers.Clear();
            }
            for (int i = 0; i < inner.Headers; i++)
            {
                var (key, value) = inner.Header(i);
                Headers.Add(key, value);
            }

            try
            {
                var query = HttpUtility.ParseQueryString(Url.Query);
                for (int i = 0; i < query.Count; i++)
                {
                    queryParameters.Add(query.GetKey(i)!, query.Get(i)!);
                }
            }
            catch
            {
                throw new WebException(Response.Text("Invalid query string", 400));
            }
        }

        public IDictionary<string, string> Parameters => parameters;
        public IDictionary<string, string> QueryParameters => queryParameters;

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

            return s is not null ? s : throw ex;
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
                value = Convert.ChangeType(parameters[name], typeof(T), CultureInfo.InvariantCulture);
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
                this.parameters.Add(name, value);
            }
        }

        public T QueryParam<T>(string name) where T : IConvertible
        {
            object? value;
            var ex = new WebException(Response.Text($"Invalid query parameter '{name}'", 400));

            try
            {
                value = Convert.ChangeType(queryParameters[name], typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                throw ex;
            }

            return value is not null ? (T) value : throw ex;
        }
    }
}
