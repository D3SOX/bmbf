using System.IO;
using System.Text;
using System.Text.Json;
using Hydra;
using MimeMapping;

namespace BMBF.WebServer;

/// <summary>
/// Convenience class for constructing <see cref="HttpResponse"/>
/// </summary>
public static class Responses
{
    /// <summary>
    /// The default <see cref="JsonSerializerOptions"/> used for JSON responses.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; set; } = new();
    
    /// <summary>
    /// Creates a response that will return a string as its body, encoded as UTF-8.
    /// </summary>
    /// <param name="body">Body value</param>
    /// <param name="status">Status code of the HTTP response</param>
    /// <param name="contentType">Used to override the default content type</param>
    /// <returns>An HTTP response with <paramref name="body"/> as its body</returns>
    public static HttpResponse Text(string body, ushort status = 200, string contentType = "text/plain; charset=utf-8") 
        => Stream(new MemoryStream(Encoding.UTF8.GetBytes(body)), status, contentType);
    
    /// <summary>
    /// Creates a response that will return JSON data.
    /// </summary>
    /// <param name="body">Instance to serialize into JSON</param>
    /// <param name="status">Status code of the response</param>
    /// <param name="overrideSerializerOptions">If not null, these <see cref="JsonSerializerOptions"/> will
    /// be used instead of <see cref="DefaultSerializerOptions"/></param>
    /// <param name="contentType">Overrides the default JSON content type</param>
    /// <typeparam name="T">Type of the instance being serialized</typeparam>
    /// <returns>An HTTP response containing <see cref="body"/> as JSON</returns>
    public static HttpResponse Json<T>(T body,
        ushort status = 200,
        JsonSerializerOptions? overrideSerializerOptions = null,
        string contentType = "application/json; charset=utf-8")
    {
        var jsonStream = new MemoryStream();
        JsonSerializer.Serialize(jsonStream, body, overrideSerializerOptions ?? DefaultSerializerOptions);
        jsonStream.Position = 0;

        return Stream(jsonStream, status, contentType);
    }

    /// <summary>
    /// Creates an empty HTTP response
    /// </summary>
    /// <param name="status">Status code of the response</param>
    /// <returns>An HTTP response with an empty body</returns>
    public static HttpResponse Empty(ushort status) => new(status)
    {
        Headers =
        {
            ["Content-Length"] = 0.ToString()
        }
    };
    

    /// <summary>
    /// Creates an HTTP response returning the content of the given file as its body.
    /// </summary>
    /// <param name="path">Path of the file containing the body</param>
    /// <returns>An HTTP response containing the content of <paramref name="path"/>, with <code>Content-Type</code>
    /// set depending on the file extension</returns>
    public static HttpResponse File(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var fileStream = System.IO.File.OpenRead(path);
        
        string contentType = MimeUtility.GetMimeMapping(path);
        return Stream(fileStream, 200, contentType);
    }
        
    /// <summary>
    /// Creates an HTTP response containing the content of the given stream as its body.
    /// This will set <code>Content-Length</code> to the length of the stream
    /// </summary>
    /// <param name="stream">Stream containing the body.</param>
    /// <param name="statusCode">HTTP status code of the response</param>
    /// <param name="contentType"><code>Content-Type</code> header of the response</param>
    /// <returns>Response containing <paramref name="stream"/> as its body</returns>
    public static HttpResponse Stream(Stream stream, ushort statusCode, string contentType) =>
        new(statusCode, stream, contentType)
        {
            Headers =
            {
                ["Content-Type"] = contentType,
                ["Content-Length"] = stream.Length.ToString()
            }
        };

    /// <summary>
    /// Creates an empty response with status code 200.
    /// </summary>
    public static HttpResponse Ok() => Empty(200);
    
    /// <summary>
    /// Creates an empty response with status code 404.
    /// </summary>
    public static HttpResponse NotFound() => Empty(404);

    /// <summary>
    /// Creates an empty response with status code 400.
    /// </summary>
    public static HttpResponse BadRequest() => Empty(400);
    
    /// <summary>
    /// Creates an empty response with status code 500.
    /// </summary>
    public static HttpResponse InternalServerError() => Empty(500);

    /// <summary>
    /// Creates a text response with status 404 and the given error as its body.
    /// </summary>
    /// <param name="error">Error to be stored in the response body as UTF-8</param>
    public static HttpResponse NotFound(string error) => Text(error, 404);
    
    /// <summary>
    /// Creates a text response with status 400 and the given error as its body.
    /// </summary>
    /// <param name="error">Error to be stored in the response body as UTF-8</param>
    public static HttpResponse BadRequest(string error) => Text(error, 400);
    
    /// <summary>
    /// Creates a text response with status 500 and the given error as its body.
    /// </summary>
    /// <param name="error">Error to be stored in the response body as UTF-8</param>
    public static HttpResponse InternalServerError(string error) => Text(error, 500);
}
