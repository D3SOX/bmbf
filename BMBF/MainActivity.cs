#nullable enable

using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V4.App;

namespace BMBF
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            ActivityCompat.RequestPermissions(this, new[]{ Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage }, 1);

            if (!BMBFService.Running)
            {
                Intent intent = new Intent(this, typeof(BMBFService));
                // TODO: Start as foreground or background depending on config option
                StartService(intent);
            }
        }
    }
}