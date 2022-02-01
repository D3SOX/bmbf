using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BMBF.Backend.Extensions;
using BMBF.Backend.Models.Setup;
using BMBF.Backend.Services;
using BMBF.Resources;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BMBF.Backend.Controllers;

[Route("[controller]")]
public class SetupController : Controller
{
    private readonly ISetupService _setupService;
    private readonly IAssetService _assetService;
    private readonly IBeatSaberService _beatSaberService;

    public SetupController(ISetupService setupService, IAssetService assetService, IBeatSaberService beatSaberService)
    {
        _setupService = setupService;
        _assetService = assetService;
        _beatSaberService = beatSaberService;
    }

    [HttpGet("[action]")]
    public async Task<ActionResult<SetupStatus>> Status()
    {
        await _setupService.LoadCurrentStatusAsync(); // Load current status from disk if not loaded already
        if (_setupService.CurrentStatus is null)
        {
            return NotFound("Setup has not yet started");
        }
        return Ok(_setupService.CurrentStatus);
    }

    [HttpGet("[action]")]
    public async Task<ActionResult<IEnumerable<string>>> ModVersions()
    {
        // Find the versions that we can currently mod
        return Ok((await _assetService.GetCoreMods(true)).Keys);
    }

    [HttpGet("[action]")]
    public async Task<ActionResult<IEnumerable<string>>> DowngradeVersions()
    {
        List<DiffInfo> diffs;
        try
        {
            diffs = await _assetService.GetDiffs(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch diffs for downgrading");
            return NotFound("Could not fetch diffs for downgrading - this is usually caused by lack of internet");
        }


        var installInfo = await _beatSaberService.GetInstallationInfoAsync();
        if (installInfo == null)
        {
            return BadRequest("Beat Saber is not installed");
        }

        var accessibleVersions = diffs
            .Select(d => d.ToVersion) // Find all versions
            .Distinct() // Remove duplicates
            .Where(version => diffs.FindShortestPath(installInfo.Version, version) != null); // Where a downgrade path exists

        return Ok(accessibleVersions);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Begin()
    {
        if (_setupService.CurrentStatus != null)
        {
            // Setup already ongoing
            return BadRequest("Setup already started");
        }

        try
        {
            await _setupService.BeginSetupAsync();
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Cannot begin setup when Beat Saber is not installed");
        }
        return Ok();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Downgrade([FromBody] string toVersion)
    {
        List<DiffInfo> diffs = await _assetService.GetDiffs();

        var installInfo = await _beatSaberService.GetInstallationInfoAsync();
        if (installInfo == null) return BadRequest("Beat Saber is not installed");
        var path = diffs.FindShortestPath(installInfo.Version, toVersion);

        if (path == null) return BadRequest("No downgrade path found");

        try
        {
            await _setupService.DowngradeAsync(path);
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Incorrect setup stage");
        }
        return Ok();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Patch()
    {
        try
        {
            await _setupService.PatchAsync();
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Incorrect setup stage");
        }
        catch (HttpRequestException)
        {
            return BadRequest("Could not download modloader, and no modloader was built-in");
        }
        
        return Ok();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> TriggerUninstall()
    {
        try
        {
            await _setupService.TriggerUninstallAsync();
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Incorrect setup stage");
        }
        return Ok();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> TriggerInstall()
    {
        try
        {
            await _setupService.TriggerInstallAsync();
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Incorrect setup stage");
        }
        return Ok();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> FinalizeSetup()
    {
        try
        {
            await _setupService.FinalizeSetup();
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Incorrect setup stage");
        }
        return Ok();
    }

    [HttpPost("quit")]
    public async Task Quit()
    {
        await _setupService.QuitSetupAsync();
    }
}
