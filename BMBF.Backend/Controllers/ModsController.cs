using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BMBF.Backend.Models;
using BMBF.Backend.Services;
using BMBF.ModManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeTypes;

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

    [HttpGet("download/{modId}")]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> DownloadMod(string modId)
    {
        if (!(await _modService.GetModsAsync()).TryGetValue(modId, out var mod))
        {
            return NotFound();
        }

        var modStream = System.IO.File.OpenRead(mod.path);
        return File(modStream, "application/octet-stream", $"{mod.mod.Id}_v{mod.mod.Version}{Path.GetExtension(mod.path)}");
    }

    [HttpGet("cover/{modId}")]
    public async Task<IActionResult> GetModCover(string modId)
    {
        if(!(await _modService.GetModsAsync()).TryGetValue(modId, out var matching))
        {
            return NotFound();
        }
        
        var coverFileName = matching.mod.CoverImageFileName;
        if (coverFileName == null)
        {
            return NotFound();
        }

        var coverExtension = Path.GetExtension(coverFileName);
        if (!MimeTypeMap.TryGetMimeType(coverExtension, out var mimeType))
        {
            return NotFound();
        }

        var coverStream = matching.mod.OpenCoverImage();
        return File(coverStream, mimeType);
    }

    [HttpPost("loadNewMods")]
    public async Task LoadNewMods()
    {
        await _modService.LoadNewModsAsync();
    }

    [HttpPost("[action]/{modId}")]
    [Produces(typeof(ModActionResult))]
    public async Task<IActionResult> Install(string modId)
    {
        return await SetInstallStatus(modId, true);
    }

    [HttpPost("[action]/{modId}")]
    [Produces(typeof(ModActionResult))]
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