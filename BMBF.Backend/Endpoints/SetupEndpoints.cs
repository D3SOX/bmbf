using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.Backend.Extensions;
using BMBF.Backend.Services;
using BMBF.Resources;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;
using Serilog;

namespace BMBF.Backend.Endpoints;

public class SetupEndpoints : IEndpoints
{
    private readonly ISetupService _setupService;
    private readonly IAssetService _assetService;
    private readonly IBeatSaberService _beatSaberService;

    public SetupEndpoints(ISetupService setupService, IAssetService assetService, IBeatSaberService beatSaberService)
    {
        _setupService = setupService;
        _assetService = assetService;
        _beatSaberService = beatSaberService;
    }

    [HttpGet("/setup/status")]
    public async Task<HttpResponse> Status()
    {
        await _setupService.LoadCurrentStatusAsync(); // Load current status from disk if not loaded already
        if (_setupService.CurrentStatus is null)
        {
            return Responses.NotFound("Setup has not yet started");
        }
        return Responses.Json(_setupService.CurrentStatus);
    }

    [HttpGet("/setup/moddableversions")]
    public async Task<HttpResponse> ModdableVersions()
    {
        var coreMods = await _assetService.GetCoreMods();
        return Responses.Json(coreMods?.coreMods.Keys ?? Enumerable.Empty<string>());
    }

    [HttpGet("/setup/accessibleversions")]
    public async Task<HttpResponse> DowngradeVersions()
    {
        var installInfo = await _beatSaberService.GetInstallationInfoAsync();
        if (installInfo == null)
        {
            return Responses.BadRequest("Beat Saber is not installed");
        }
        
        List<DiffInfo> diffs;
        try
        {
            diffs = await _assetService.GetDiffs();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch diffs for downgrading");
            return Responses.NotFound("Could not fetch diffs for downgrading - " +
                "this is usually caused by lack of internet");
        }

        var accessibleVersions = diffs
            .Select(d => d.ToVersion) // Find all versions
            .Distinct() // Remove duplicates
            .Where(version => diffs.FindShortestPath(installInfo.Version, version) != null); // Where a downgrade path exists

        return Responses.Json(accessibleVersions);
    }

    [HttpPost("/setup/begin")]
    public async Task<HttpResponse> Begin()
    {
        try
        {
            await _setupService.BeginSetupAsync();
        }
        catch (InvalidOperationException)
        {
            return Responses.BadRequest("Cannot begin setup when Beat Saber is not installed");
        }
        catch (InvalidStageException)
        {
            return Responses.BadRequest("Setup has already started");
        }
        
        return Responses.Ok();
    }

    [HttpPost("/setup/downgrade")]
    public async Task<HttpResponse> Downgrade(Request request)
    {
        string toVersion = request.JsonBody<string>();
        List<DiffInfo> diffs;
        try
        {
            diffs = await _assetService.GetDiffs();
        }
        catch (HttpRequestException ex)
        {
            return Responses.InternalServerError($"Could not download diff index: {ex.Message}");
        }

        var installInfo = await _beatSaberService.GetInstallationInfoAsync();
        if (installInfo == null)
        {
            return Responses.BadRequest("Beat Saber is not installed");
        }
        var path = diffs.FindShortestPath(installInfo.Version, toVersion);

        if (path == null)
        {
            return Responses.BadRequest("No downgrade path found");
        }

        try
        {
            await _setupService.DowngradeAsync(path);
        }
        catch (InvalidStageException)
        {
            return Responses.BadRequest("Incorrect setup stage");
        }
        return Responses.Ok();
    }

    [HttpPost("/setup/patch")]
    public async Task<HttpResponse> Patch()
    {
        try
        {
            await _setupService.PatchAsync();
        }
        catch (InvalidStageException)
        {
            return Responses.BadRequest("Incorrect setup stage");
        }
        catch (HttpRequestException)
        {
            return Responses.InternalServerError("Could not download modloader, and no modloader was built-in");
        }

        return Responses.Ok();
    }

    [HttpPost("/setup/triggeruninstall")]
    public async Task<HttpResponse> TriggerUninstall()
    {
        try
        {
            await _setupService.TriggerUninstallAsync();
        }
        catch (InvalidStageException)
        {
            return Responses.BadRequest("Incorrect setup stage");
        }
        return Responses.Ok();
    }

    [HttpPost("/setup/triggerinstall")]
    public async Task<HttpResponse> TriggerInstall()
    {
        try
        {
            await _setupService.TriggerInstallAsync();
        }
        catch (InvalidStageException)
        {
            return Responses.BadRequest("Incorrect setup stage");
        }
        return Responses.Ok();
    }

    [HttpPost("/setup/finalize")]
    public async Task<HttpResponse> FinalizeSetup()
    {
        try
        {
            await _setupService.FinalizeSetup();
        }
        catch (InvalidStageException)
        {
            return Responses.BadRequest("Incorrect setup stage");
        }
        return Responses.Ok();
    }

    [HttpPost("/setup/quit")]
    public async Task Quit()
    {
        await _setupService.QuitSetupAsync();
    }

}
