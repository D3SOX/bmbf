using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.Backend.Endpoints;

public class SyncSaberEndpoints : IEndpoints
{
    private readonly ISyncSaberService _syncSaberService;

    public SyncSaberEndpoints(ISyncSaberService syncSaberService)
    {
        _syncSaberService = syncSaberService;
    }

    [HttpGet("/syncsaber/config")]
    public async Task<HttpResponse> GetSyncSaberConfig()
    {
        return Responses.Json(await _syncSaberService.GetConfig());
    }

    [HttpPut("/syncsaber/config")]
    public async Task PutSyncSaberConfig(Request req)
    {
        await _syncSaberService.OverwriteConfig(req.JsonBody<SyncSaberConfig>());
    }

    [HttpPost("/syncsaber/sync")]
    public async Task Sync()
    {
        await _syncSaberService.Sync();
    }
}
