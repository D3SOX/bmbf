namespace BMBF;

public static class Constants
{
    public const string LogPath = "/sdcard/BMBFService.log";
    public const string BindPort = "50005";
    public const string BindAddress = $"http://0.0.0.0:{BindPort}";
    public const string RunForegroundConfig = "/sdcard/BMBFData/powerlock.enabled";
    public const string WebRootPath = "wwwroot";
}