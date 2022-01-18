using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.ModManagement;
using Microsoft.AspNetCore.Mvc;

namespace BMBF.Backend.Controllers;

[Route("[controller]")]
public class ModsController : Controller
{
    private readonly IModService _modService;

    public ModsController(IModService modService)
    {
        _modService = modService;
    }

    [HttpGet]
    public async Task<IEnumerable<IMod>> GetMods()
    {
        return (await _modService.GetModsAsync()).Values.Select(pair => pair.mod);
    }

    [HttpPost("loadNewMods")]
    public async Task LoadNewMods()
    {
        await _modService.LoadNewModsAsync();
    }

    [HttpPost("[action]/{modId}")]
    public async Task<IActionResult> Install(string modId)
    {
        return await SetInstallStatus(modId, true);
    }

    [HttpPost("[action]/{modId}")]
    public async Task<IActionResult> Uninstall(string modId)
    {
        return await SetInstallStatus(modId, false);
    }
    
    private async Task<IActionResult> SetInstallStatus(string id, bool installed)
    {
        var mods = await _modService.GetModsAsync();
        if (!mods.TryGetValue(id, out var modPair))
        {
            return NotFound();
        }

        var mod = modPair.mod;
        try
        {
            if (installed)
            {
                await mod.InstallAsync();
            }
            else
            {
                await mod.UninstallAsync();
            }
            return Ok(new ModActionResult());
        }
        catch (InstallationException ex)
        {
            return Ok(new ModActionResult
            {
                Error = ex.Message
            });
        }
    }
}