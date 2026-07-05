using Android.Content;
using AndroidX.Camera.Core;
using CarPlates.Mobile.Controls;

namespace CarPlates.Mobile.Platforms.Android.Handlers;

public class PlateAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    private readonly Context _context;
    private readonly CameraPreview _cameraPreview;

    public PlateAnalyzer(Context context, CameraPreview cameraPreview)
    {
        _context = context;
        _cameraPreview = cameraPreview;
    }

    public void Analyze(IImageProxy imageProxy)
    {
        // Stub: close the image without processing
        imageProxy.Close();
    }
}
