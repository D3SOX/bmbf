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

    public ImportEndpoints(IFileImporter fileImporter)
    {
        _fileImporter = fileImporter;
    }

    [HttpPost("/import")]
    public async Task<HttpResponse> ImportFile(Request request)
    {
        if (!request.Headers.TryGetValue("filename", out var fileName))
        {
            return Responses.BadRequest("Cannot import file without filename");
        }

        var result = await _fileImporter.TryImportAsync(request.Body, fileName);
        if (result.Type == FileImportResultType.Failed)
        {
            Log.Error($"Failed to import file {fileName}: {result.Error}");
        }
        return Responses.Json(result);
    }
}
