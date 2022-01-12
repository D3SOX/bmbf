using System;
using Android.Content;

namespace BMBF;

/// <summary>
/// Listens for the webserver startup broadcast
/// </summary>
public class WebServerStartedReceiver : BroadcastReceiver
{
    /// <summary>
    /// Invoked when the web server finishes starting up
    /// Argument is the web server address
    /// </summary>
    public event EventHandler<string>? WebServerStartupComplete;

    /// <summary>
    /// Invoked if the web server fails to start
    /// Argument is the exception thrown
    /// </summary>
    public event EventHandler<string>? WebServerStartupFailed;

    /// <summary>
    /// Invoked to trigger a package install, since only activities can do this
    /// Argument is APK path
    /// </summary>
    public event EventHandler<string>? PackageInstallTriggered;
        
    /// <summary>
    /// Invoked to trigger a package uninstall, since only activities can do this
    /// Argument is package ID
    /// </summary>
    public event EventHandler<string>? PackageUninstallTriggered;

    /// <summary>
    /// Invoked when the service requests the activity to quit
    /// </summary>
    public event EventHandler? Quit;

    /// <summary>
    /// Invoked when the service requests the activity to restart itself
    /// </summary>
    public event EventHandler? Restart;
        
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent == null)
        {
            return;
        }
            
        if (intent.Action == BMBFIntents.WebServerStartedIntent)
        {
            WebServerStartupComplete?.Invoke(this, intent.GetStringExtra("BindAddress")!);
        }   else if (intent.Action == BMBFIntents.WebServerFailedToStartIntent)
        {
            WebServerStartupFailed?.Invoke(this, intent.GetStringExtra("Exception")!);
        }   else if (intent.Action == BMBFIntents.TriggerPackageInstall)
        {
            PackageInstallTriggered?.Invoke(this, intent.GetStringExtra("ApkPath")!);
        }   else if (intent.Action == BMBFIntents.TriggerPackageUninstall)
        {
            PackageUninstallTriggered?.Invoke(this, intent.GetStringExtra("PackageId")!);
        }   else if (intent.Action == BMBFIntents.Quit)
        {
            Quit?.Invoke(this, EventArgs.Empty);
        }   else if (intent.Action == BMBFIntents.Restart)
        {
            Restart?.Invoke(this, EventArgs.Empty);
        }
    }
}