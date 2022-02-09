using MimeMapping;
using NetCoreServer;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BMBF.WebServer
{
    public class Response
    {
        public byte[] Body;
        public ushort Status;
        public readonly HttpResponseHeaders Headers;

        public Response(byte[] body, ushort status = 200, string? contentType = "application/octet-stream")
        {
            Body = body;
            Status = status;

            using var tmp = new HttpResponseMessage();
            Headers = tmp.Headers;
            Headers.Clear();
            if (contentType is not null)
            {
                Headers.Add("Content-Type", contentType);
            }
        }

        public static Response Text(string body, ushort status = 200, string contentType = "text/plain; charset=utf-8") =>
            new Response(Encoding.UTF8.GetBytes(body), status, contentType);
        public static Response Json<T>(T body, ushort status = 200, string contentType = "application/json; charset=utf-8") =>
            new Response(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)), status, contentType);
        public static Response Empty(ushort status = 204) => new Response(Array.Empty<byte>(), status, null);
        public static async Task<Response> File(string path)
        {
            if (!System.IO.File.Exists(path)) return Text("Not Found", 404);

            var body = await System.IO.File.ReadAllBytesAsync(path)!;
            var contentType = MimeUtility.GetMimeMapping(path);

            return new Response(body, 200, contentType);
        }

        public Task<Response> Async() => Task.FromResult(this);

        internal HttpResponse ToInner(HttpResponse response)
        {
            response.Clear();
            response.SetBegin(Status);
            foreach (var (name, values) in Headers)
            {
                response.SetHeader(name, string.Join(',', values));
            }
            if (Body.Length > 0)
            {
                response.SetBody(Body);
            }
            return response;
        }
    }
}
