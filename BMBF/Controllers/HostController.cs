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

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Version != null ? Ok(Version) : NotFound();
    }

    [HttpPost("quit")]
    public void Quit()
    {
        // Tell frontend to quit too
        Intent intent = new Intent(BMBFIntents.Quit);
        _bmbfService.SendBroadcast(intent);
            
        // Actually stop BMBFService
        _bmbfService.StopSelf();
    }

    [HttpPost("restart")]
    public void Restart()
    {
        // Tell frontend to restart BMBFService
        Intent intent = new Intent(BMBFIntents.Restart);
        _bmbfService.SendBroadcast(intent);
    }

    [HttpPost("runInBackground")]
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

    [HttpGet("runInBackground")]
    public bool GetRunInBackground()
    {
        return System.IO.File.Exists(Constants.RunForegroundConfig);
    }

    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        var logsPath = Constants.LogPath;
        if (System.IO.File.Exists(logsPath))
        {
            var logsStream = System.IO.File.OpenRead(logsPath);
            return File(logsStream, "text/plain");
        }

        return NotFound();
    }
}