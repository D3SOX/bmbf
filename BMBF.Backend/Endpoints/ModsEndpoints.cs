using System.IO.Abstractions;
using BMBF.Backend.Services;
using BMBF.ModManagement;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MimeTypes;

namespace BMBF.Backend.Endpoints;

public class ModsEndpoints : IEndpoints
{
    private readonly IModService _modService;
    private readonly ICoreModService _coreModService;
    private readonly IFileSystem _io;

    public ModsEndpoints(IModService modService, ICoreModService coreModService, IFileSystem io)
    {
        _modService = modService;
        _coreModService = coreModService;
        _io = io;
    }

    [HttpGet("/mods")]
    public async Task<HttpResponse> GetMods()
    {
        var mods = (await _modService.GetModsAsync()).Values.Select(pair => pair.mod);
        return Responses.Json(mods);
    }
    

    [HttpGet("/mods/download/{id}")]
    public async Task<HttpResponse> DownloadMod(Request request)
    {
        if (!(await _modService.GetModsAsync())
            .TryGetValue(request.Param<string>("id"), out var mod))
        {
            return Responses.NotFound();
        }

        var modStream = _io.File.OpenRead(mod.path);
        
        var response = Responses.Stream(modStream, 200, "application/octet-stream");
        
        string fileName = $"{mod.mod.Id}_v{mod.mod.Version}{Path.GetExtension(mod.path)}";
        response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
        return response;
    }

    [HttpGet("/mods/cover/{id}")]
    public async Task<HttpResponse> GetModCover(Request request)
    {
        if (!(await _modService.GetModsAsync())
            .TryGetValue(request.Param<string>("id"), out var matching))
        {
            return Responses.NotFound();
        }

        string? coverFileName = matching.mod.CoverImageFileName;
        if (coverFileName == null)
        {
            return Responses.NotFound();
        }

        string coverExtension = Path.GetExtension(coverFileName);
        if (!MimeTypeMap.TryGetMimeType(coverExtension, out var mimeType))
        {
            return Responses.NotFound();
        }

        var coverStream = matching.mod.OpenCoverImage();
        return new HttpResponse(200, coverStream, mimeType)
        {
            Headers =
            {
                ["Content-Type"] = mimeType,
            }
        };
    }

    [HttpPost("/mods/loadNew")]
    public async Task LoadNewMods()
    {
        await _modService.LoadNewModsAsync();
    }

    [HttpPost("/mods/install/{id}")]
    public async Task<HttpResponse> Install(Request request)
    {
        return await SetInstallStatus(request.Param<string>("id"), true);
    }

    [HttpPost("/mods/uninstall/{id}")]
    public async Task<HttpResponse> Uninstall(Request request)
    {
        return await SetInstallStatus(request.Param<string>("id"), false);
    }

    private async Task<HttpResponse> SetInstallStatus(string id, bool installed)
    {
        var mods = await _modService.GetModsAsync();
        if (!mods.TryGetValue(id, out var modPair))
        {
            return Responses.NotFound();
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
            return Responses.Ok();
        }
        catch (InstallationException ex)
        {
            return Responses.BadRequest(ex.Message);
        }
    }
    
    [HttpPost("/mods/installcore")]
    public async Task<HttpResponse> InstallCore()
    {
        return Responses.Json(await _coreModService.InstallAsync(true));
    }

    [HttpPost("/mods/updatestatuses")]
    public async Task UpdateStatuses()
    {
        await _modService.UpdateModStatusesAsync();
    }
}
