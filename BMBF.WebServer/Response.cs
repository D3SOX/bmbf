using MimeMapping;
using NetCoreServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BMBF.WebServer
{
    public class Response
    {
        public byte[] Body { get; set; }
        public ushort Status { get; set; }

        private readonly HeaderDictionary _headers = new();
        public IDictionary<string, string> Headers => _headers;

        public Response(byte[] body, ushort status = 200, string? contentType = "application/octet-stream")
        {
            Body = body;
            Status = status;

            if (contentType is not null)
            {
                _headers["Content-Type"] = contentType;
            }
        }

        public static JsonSerializerOptions SerializerOptions { get; set; } = new();

        public static Response Text(string body, ushort status = 200, string contentType = "text/plain; charset=utf-8") =>
            new(Encoding.UTF8.GetBytes(body), status, contentType);
        public static Response Json<T>(T body, ushort status = 200, string contentType = "application/json; charset=utf-8") =>
            new(JsonSerializer.SerializeToUtf8Bytes(body, SerializerOptions), status, contentType);
        public static Response Empty(ushort status = 204) => new(Array.Empty<byte>(), status, null);
        public static async Task<Response> File(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return Text("Not Found", 404);
            }

            byte[] body = await System.IO.File.ReadAllBytesAsync(path);
            string? contentType = MimeUtility.GetMimeMapping(path);

            return new Response(body, 200, contentType);
        }
        
        public static async Task<Response> Stream(Stream stream, string contentType)
        {
            using var bodyStream = new MemoryStream();
            await stream.CopyToAsync(bodyStream);
            
            return new Response(bodyStream.ToArray(), 200, contentType);
        }

        public Task<Response> Async() => Task.FromResult(this);

        internal HttpResponse ToInner(HttpResponse response)
        {
            response.Clear();
            response.SetBegin(Status);
            foreach (var (name, value) in _headers)
            {
                response.SetHeader(name, value);
            }
            response.SetBody(Body);
            return response;
        }
    }
}
