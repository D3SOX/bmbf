using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Hydra;

namespace BMBF.WebServer;

/// <summary>
/// Convenience class for working with HTTP requests
/// </summary>
public class Request
{
    /// <summary>
    /// Routing path of this request (i.e. the absolute path of the URI - without query parameters)
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Parameters given to this request in the request path (i.e. using {myParam})
    /// </summary>
    public IDictionary<string, string> Parameters => _parameters;

    /// <summary>
    /// Query parameters in the request URI.
    /// </summary>
    public IDictionary<string, string> QueryParameters => _queryParameters;

    /// <summary>
    /// HTTP method of the request
    /// </summary>
    public HttpMethod ParsedMethod { get; }

    /// <summary>
    /// HTTP request this wrapper is based upon
    /// </summary>
    public HttpRequest Inner { get; }

    /// <summary>
    /// Body of the request
    /// </summary>
    public Stream Body => Inner.Body;

    /// <summary>
    /// Headers of the request
    /// </summary>
    public ReadOnlyHttpHeaders Headers => Inner.Headers;

    private readonly Dictionary<string, string> _queryParameters = new();
    private readonly Dictionary<string, string> _parameters = new();

    internal Request(HttpRequest inner)
    {
        Inner = inner;

        // Create a temporary URI for parsing purposes
        var uri = new Uri($"http://127.0.0.1{inner.Uri}");
        Path = uri.AbsolutePath;
        ParsedMethod = new HttpMethod(inner.Method);

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
            throw new WebException(Responses.Text("Invalid query string", 400));
        }
    }

    /// <summary>
    /// Reads the body of this request as UTF-8.
    /// </summary>
    /// <returns>The body of this request</returns>
    /// <exception cref="WebException">If the body is not valid UTF-8</exception>
    public async Task<string> TextBody()
    {
        string? s;
        var ex = new WebException(Responses.BadRequest("Invalid UTF8 request body"));

        try
        {
            using var bodyReader = new StreamReader(Body, System.Text.Encoding.UTF8);
            s = await bodyReader.ReadToEndAsync();
        }
        catch
        {
            throw ex;
        }

        return s ?? throw ex;
    }

    /// <summary>
    /// Deserializes the body of the request as JSON
    /// </summary>
    /// <typeparam name="T">Type of the JSON to deserialize to</typeparam>
    /// <returns>The body deserialized from JSON</returns>
    /// <exception cref="WebException">If the body is not valid JSON</exception>
    public async Task<T> JsonBody<T>()
    {
        T? json;

        try
        {
            json = await JsonSerializer.DeserializeAsync<T>(Body);
        }
        catch (JsonException ex)
        {
            throw new WebException(Responses.BadRequest(ex.Message));
        }

        return json is not null ? json : throw new WebException(Responses.BadRequest("Invalid JSON request body"));
    }

    /// <summary>
    /// Gets a path parameter from <see cref="Path"/>
    /// </summary>
    /// <param name="name">Name of the parameter, not including the {}</param>
    /// <typeparam name="T">Type to convert the parameter into</typeparam>
    /// <returns>The request parameter, casted to <typeparamref name="T"/></returns>
    /// <exception cref="WebException">If the path parameter is missing, or cannot be converted to
    /// <typeparamref name="T"/></exception>
    public T Param<T>(string name) where T : IConvertible
    {
        try
        {
            return (T) Convert.ChangeType(_parameters[name], typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            throw new WebException(Responses.BadRequest($"Invalid path parameter '{name}'"));
        }
    }

    /// <summary>
    /// Gets a query parameter from the request URI
    /// </summary>
    /// <param name="name">Name of the parameter, not including the {}</param>
    /// <typeparam name="T">Type to convert the parameter into</typeparam>
    /// <returns>The request parameter, casted to <typeparamref name="T"/></returns>
    /// <exception cref="WebException">If the path parameter is missing, or cannot be converted to
    /// <typeparamref name="T"/></exception>
    public T QueryParam<T>(string name) where T : IConvertible
    {
        try
        {
            return (T) Convert.ChangeType(_queryParameters[name], typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            throw new WebException(Responses.BadRequest($"Invalid query parameter '{name}'"));
        }
    }

    internal void AddParams(IDictionary<string, string> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            _parameters.Add(name, value);
        }
    }
}
