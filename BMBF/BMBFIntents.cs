namespace BMBF;

/// <summary>
/// Stores BMBF intents used to communicate between the activity and service
/// </summary>
// ReSharper disable once InconsistentNaming
public static class BMBFIntents
{
    public const string WebServerStartedIntent = "BMBFWebServerStarted";

    public const string WebServerFailedToStartIntent = "BMBFWebServerFailedToStart";

    public const string TriggerPackageInstall = "BMBFTriggerPackageInstall";

    public const string TriggerPackageUninstall = "BMBFTriggerPackageUninstall";

    public const string TriggerPackageLaunch = "BMBFTriggerPackageLaunch";

    public const string Quit = "BMBFQuit";

    public const string Restart = "BMBFRestart";
}
