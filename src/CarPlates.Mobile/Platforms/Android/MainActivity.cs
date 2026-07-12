using Android.App;
using Android.Content.PM;
using Android.OS;
using CarPlates.Application.Common.Interfaces;
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

        // The document scanner's ActivityResultLauncher must be registered
        // before this Activity is started, so resolve the singleton now
        // rather than waiting for the Scanner page to request it.
        IPlatformApplication.Current?.Services.GetService<IDocumentScannerService>();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [Android.Runtime.GeneratedEnum] Permission[] grantResults)
    {
        Microsoft.Maui.ApplicationModel.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
