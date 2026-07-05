using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace CarPlates.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", 
          MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Request camera permission on startup
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            RequestPermissions(new[] { 
                Android.Manifest.Permission.Camera,
                Android.Manifest.Permission.Internet,
                Android.Manifest.Permission.AccessNetworkState,
                Android.Manifest.Permission.WriteExternalStorage
            }, 0);
        }
    }
}
