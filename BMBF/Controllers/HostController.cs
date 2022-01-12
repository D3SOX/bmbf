using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
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

    private readonly Service _bmbfService;

    public HostController(Service bmbfService)
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
    public void Quit()
    {
        // Tell frontend to quit too
        Intent intent = new Intent(BMBFIntents.Quit);
        _bmbfService.SendBroadcast(intent);
            
        // Actually stop BMBFService
        _bmbfService.StopSelf();
    }

    [HttpPost]
    [Route("restart")]
    public void Restart()
    {
        // Tell frontend to restart BMBFService
        Intent intent = new Intent(BMBFIntents.Restart);
        _bmbfService.SendBroadcast(intent);
    }

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