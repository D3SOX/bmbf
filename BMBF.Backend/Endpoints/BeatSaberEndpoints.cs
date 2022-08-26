using System.Threading.Tasks;
using BMBF.Backend.Services;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.Backend.Endpoints;

public class BeatSaberEndpoints : IEndpoints
{
    private readonly IBeatSaberService _beatSaberService;

    public BeatSaberEndpoints(IBeatSaberService beatSaberService)
    {
        _beatSaberService = beatSaberService;
    }

    [HttpGet("/beatsaber/install")]
    public async Task<HttpResponse> GetInstallInfo()
    {
        var installInfo = await _beatSaberService.GetInstallationInfoAsync();
        if (installInfo is null)
        {
            return Responses.NotFound("Beat Saber is not installed");
        }
        return Responses.Json(installInfo);
    }

    [HttpPost("/beatsaber/launch")]
    public void Launch()
    {
        _beatSaberService.Launch();
    }
}
