#nullable enable

using System;
using System.Threading;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V4.App;
using Android.Webkit;
using Android.Widget;

namespace BMBF
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private WebServerStartedReceiver? _startupReceiver;
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_loading);

            ActivityCompat.RequestPermissions(this, new[]{ Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage }, 1);
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            // Register a receiver for when the web server finishes starting up
            IntentFilter intentFilter = new IntentFilter();
            intentFilter.AddAction(BMBFIntents.WebServerStartedIntent);
            intentFilter.AddAction(BMBFIntents.WebServerFailedToStartIntent);
            if (_startupReceiver == null)
            {
                _startupReceiver = new WebServerStartedReceiver();
                // Navigate to the main page when WebServer startup finishes
                _startupReceiver.WebServerStartupComplete +=
                    (sender, url) => RunOnUiThread(() => OnLoaded(url));
                
                // Make sure to inform of errors
                _startupReceiver.WebServerStartupFailed +=
                    (sender, error) => RunOnUiThread(() => OnFailedToLoad(error));
            }
            RegisterReceiver(_startupReceiver, intentFilter);
            
            if (BMBFService.RunningUrl == null)
            {
                Intent intent = new Intent(this, typeof(BMBFService));
                // TODO: Start as foreground or background depending on config option
                StartService(intent);
            }
            else
            {
                OnLoaded(BMBFService.RunningUrl);
            }
        }

        private void OnLoaded(string url)
        {
            SetContentView(Resource.Layout.activity_main);
            WebView webView = FindViewById<WebView>(Resource.Id.webView) ?? throw new NullReferenceException(nameof(WebView));
            webView.LoadUrl(url);
        }

        private void OnFailedToLoad(string error)
        {
            SetContentView(Resource.Layout.startup_failed);
            TextView textView = FindViewById<TextView>(Resource.Id.exception) ?? throw new NullReferenceException(nameof(TextView));
            textView.Text = error;
        }

        protected override void OnPause()
        {
            base.OnPause();
            UnregisterReceiver(_startupReceiver);
        }
    }
}