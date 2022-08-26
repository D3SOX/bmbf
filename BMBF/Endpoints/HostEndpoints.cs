using Android.App;
using Android.Content;
using BMBF.Backend.Endpoints;
using BMBF.WebServer;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.Endpoints;

public class HostEndpoints : IEndpoints
{
    private readonly Service _bmbfService;

    private bool RunAsForegroundService
    {
        get => System.IO.File.Exists(Constants.RunForegroundConfig);
        set
        {
            if (RunAsForegroundService && !value)
            {
                System.IO.File.Delete(Constants.RunForegroundConfig);
            }
            else if (!RunAsForegroundService && value)
            {
                System.IO.File.WriteAllText(Constants.RunForegroundConfig, "");
            }
        }
    }

    public HostEndpoints(Service bmbfService)
    {
        _bmbfService = bmbfService;
    }

    [HttpPost("/quit")]
    public void Quit()
    {
        // Tell frontend to quit too
        var intent = new Intent(BMBFIntents.Quit);
        _bmbfService.SendBroadcast(intent);

        // Actually stop BMBFService
        _bmbfService.StopSelf();
    }

    [HttpPost("/restart")]
    public void Restart()
    {
        // Tell frontend to restart BMBFService
        var intent = new Intent(BMBFIntents.Restart);
        _bmbfService.SendBroadcast(intent);
    }

    [HttpPost("/runInBackground")]
    public void SetRunInBackground(Request request)
    {
        RunAsForegroundService = request.JsonBody<bool>();
    }

    [HttpGet("/runInBackground")]
    public HttpResponse GetRunInBackground()
    {
        return Responses.Json(RunAsForegroundService);
    }

    [HttpGet("/logs")]
    public HttpResponse GetLogs()
    {
        string logsPath = Constants.LogPath;
        if (System.IO.File.Exists(logsPath))
        {
            return Responses.File(logsPath);
        }

        return Responses.NotFound();
    }

}
