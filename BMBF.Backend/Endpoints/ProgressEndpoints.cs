using BMBF.Backend.Services;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.Backend.Endpoints;

public class ProgressEndpoints : IEndpoints
{
    private readonly IProgressService _progressService;

    public ProgressEndpoints(IProgressService progressService)
    {
        _progressService = progressService;
    }

    [HttpGet("/progress")]
    public HttpResponse GetOperations()
    {
        return Responses.Json(_progressService.CurrentOperations.Values);
    }
}
