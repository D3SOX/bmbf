using NetCoreServer;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BMBF.WebServer
{
    public class Response
    {
        public readonly byte[] Body;
        public ushort Status;
        public readonly HttpResponseHeaders Headers;

        public Response(byte[] body, ushort status = 200, string contentType = "application/octet-stream")
        {
            Body = body;
            Status = status;

            using var tmp = new HttpResponseMessage();
            Headers = tmp.Headers;
            Headers.Clear();
            if (body.Length > 0)
            {
                Headers.Add("Content-Type", contentType);
            }
        }

        public static Response Text(string body, ushort status = 200, string contentType = "text/plain; charset=utf-8") =>
            new Response(Encoding.UTF8.GetBytes(body), status, contentType);
        public static Response Json<T>(T body, ushort status = 200, string contentType = "application/json; charset=utf-8") =>
            new Response(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)), status, contentType);

        internal HttpResponse ToInner(HttpResponse response)
        {
            response.Clear();
            response.SetBegin(Status);
            response.SetBody(Body);
            foreach (var (name, values) in Headers)
            {
                response.SetHeader(name, string.Join(',', values));
            }
            return response;
        }
    }
}
