using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;
using Serilog;

namespace BMBF.Backend.Endpoints;

public class ImportEndpoints : IEndpoints
{
    private readonly IFileImporter _fileImporter;
    private readonly HttpClient _client;

    public ImportEndpoints(IFileImporter fileImporter, HttpClient client)
    {
        _fileImporter = fileImporter;
        _client = client;
    }

    private bool TryParseFileName(string dispositionStr, [MaybeNullWhen(false)] out string fileName)
    {
        if (ContentDispositionHeaderValue.TryParse(dispositionStr, out var disposition) &&
            disposition.FileName is { Length: >= 2 })
        {
            fileName = disposition.FileName[1..^1];
            return true;
        }

        fileName = null;
        return false;
    }

    [HttpPost("/import/file")]
    public async Task<HttpResponse> ImportFile(Request request)
    {
        // Attempt to parse the filename from the headers
        if (!request.Headers.TryGetValue("Content-Disposition", out var disposition)
           || !TryParseFileName(disposition, out string? fileName))
        {
            return Responses.BadRequest("Cannot import file without filename");
        }

        var result = await _fileImporter.TryImportAsync(request.Body, fileName);
        if (result.Type == FileImportResultType.Failed)
        {
            Log.Error($"Failed to import file {fileName}: {result.Error}");
            return Responses.Json(result, 400);
        }
        else
        {
            return Responses.Json(result, 200);
        }
    }

    [HttpPost("/import/url")]
    public async Task<HttpResponse> ImportFromUrl(Request request)
    {
        var url = new Uri(request.JsonBody<string>());
        try
        {
            using var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!resp.Headers.TryGetValues("Content-Disposition", out var disposition)
                || !TryParseFileName(disposition.First(), out string? fileName))
            {
                // No file name, we will assume this based on the URL
                fileName = Path.GetFileName(url.AbsolutePath);
            }

            var result = await _fileImporter.TryImportAsync(await resp.Content.ReadAsStreamAsync(), fileName);
            if (result.Type == FileImportResultType.Failed)
            {
                Log.Error($"Failed to import file {fileName}: {result.Error}");
            }
            return Responses.Json(result);
        }
        catch (HttpRequestException ex)
        {
            return Responses.InternalServerError("Failed to import from URL: " + ex.Message);
        }
    }
}
