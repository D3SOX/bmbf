using System;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using AndroidX.Core.Content;
using File = System.IO.File;

namespace BMBF
{
    [Activity(Name = "com.weareneutralaboutoculus.BMBF.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private WebServerStartedReceiver? _receiver;
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_loading);

            RequestPermissions(new[]{ Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage }, 1);
        }

        protected override void OnResume()
        {
            base.OnResume();
            // Register a receiver for when the web server finishes starting up
            IntentFilter intentFilter = new IntentFilter();
            intentFilter.AddAction(BMBFIntents.WebServerStartedIntent);
            intentFilter.AddAction(BMBFIntents.WebServerFailedToStartIntent);
            intentFilter.AddAction(BMBFIntents.TriggerPackageInstall);
            intentFilter.AddAction(BMBFIntents.TriggerPackageUninstall);
            intentFilter.AddAction(BMBFIntents.Quit);
            intentFilter.AddAction(BMBFIntents.Restart);
            if (_receiver == null)
            {
                _receiver = new WebServerStartedReceiver();
                // Navigate to the main page when WebServer startup finishes
                _receiver.WebServerStartupComplete +=
                    (_, url) => RunOnUiThread(() => OnLoaded(url));
                
                // Make sure to inform of errors
                _receiver.WebServerStartupFailed +=
                    (_, error) => RunOnUiThread(() => OnFailedToLoad(error));

                _receiver.Quit += (_, _) => Finish();

                _receiver.PackageInstallTriggered += (_, apkPath) => TriggerPackageInstall(apkPath);
                _receiver.PackageUninstallTriggered += (_, packageId) => TriggerPackageUninstall(packageId);
                _receiver.Restart += (_, _) => Restart();
            }
            RegisterReceiver(_receiver, intentFilter);
            
            if (BMBFService.RunningUrl == null)
            {
                StartMainService();
            }
            else
            {
                OnLoaded(BMBFService.RunningUrl);
            }
        }

        private void TriggerPackageInstall(string apkPath)
        {
            var intent = new Intent(Intent.ActionView);
            var apkUri = FileProvider.GetUriForFile(this, PackageName + ".provider", new Java.IO.File(apkPath));
            intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            StartActivity(intent);
        }

        private void TriggerPackageUninstall(string packageId)
        {
            Intent intent = new Intent(
                Intent.ActionDelete,
                Android.Net.Uri.FromParts("package", packageId, null)
            );
            StartActivity(intent);
        }

        private void StartMainService()
        {
            Intent intent = new Intent(this, typeof(BMBFService));
            if (File.Exists(Constants.RunForegroundConfig))
            {
                StartForegroundService(intent);
            }
            else
            {
                StartService(intent);
            }
        }

        private void Restart()
        {
            // Move back to the loading view while the service restarts
            SetContentView(Resource.Layout.activity_loading);
            
            // Restart the service, which will send a broadcast to navigate us back to the frontend page
            StopService(new Intent(this, typeof(BMBFService)));
            StartMainService();
        }

        private void OnLoaded(string url)
        {
            SetContentView(Resource.Layout.activity_main);
            WebView webView = FindViewById<WebView>(Resource.Id.webView) ?? throw new NullReferenceException(nameof(WebView));
            if (webView.Settings == null) throw new NullReferenceException(nameof(webView.Settings));
            webView.Settings.JavaScriptEnabled = true;
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
            UnregisterReceiver(_receiver);
        }
    }
}