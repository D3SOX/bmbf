using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using BMBF.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BMBF.Controllers;

[Route("[controller]")]
public class HostController : Controller
{
    // This is static to avoid reflecting multiple times
    private static readonly string? Version;
    static HostController()
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        Version = assemblyVersion == null ? null : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
    }

    private readonly IBMBFService _bmbfService;

    public HostController(IBMBFService bmbfService)
    {
        _bmbfService = bmbfService;
    }

    [HttpGet]
    [Route("version")]
    public IActionResult GetVersion()
    {
        return Version != null ? Ok(Version) : NotFound();
    }

    [HttpPost]
    [Route("quit")]
    public void Quit() => _bmbfService.Quit();

    [HttpPost]
    [Route("restart")]
    public void Restart() => _bmbfService.Restart();

    [HttpPost]
    [Route("runInBackground")]
    public void SetRunInBackground([FromBody] bool runInBackground)
    {
        bool currentlyEnabled = GetRunInBackground();
        if (currentlyEnabled && !runInBackground)
        {
            Log.Information("Running in background disabled");
            System.IO.File.Delete(Constants.RunForegroundConfig);
        }
        else if(!currentlyEnabled && runInBackground)
        {
            Log.Information("Running in background enabled");
            System.IO.File.Create(Constants.RunForegroundConfig).Dispose();
        }
    }

    [HttpGet]
    [Route("runInBackground")]
    public bool GetRunInBackground()
    {
        return System.IO.File.Exists(Constants.RunForegroundConfig);
    }

    [HttpGet]
    [Route("logs")]
    public async Task GetLogs()
    {
        var logsPath = Constants.LogPath;
        if (System.IO.File.Exists(logsPath))
        {
            HttpContext.Response.StatusCode = (int) HttpStatusCode.OK;
            HttpContext.Response.ContentType = "text/plain";
            await using var logsStream = System.IO.File.OpenRead(logsPath);
            await logsStream.CopyToAsync(HttpContext.Response.Body);
            return;
        }
            
        HttpContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
    }
}