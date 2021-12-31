using System;
using Android.Content;

namespace BMBF
{
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
            }
        }
    }
}