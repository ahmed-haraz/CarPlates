using Android.Content;
using AndroidX.Camera.Core;
using CarPlates.Mobile.Controls;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;

namespace CarPlates.Mobile.Platforms.Android.Handlers;

public class PlateAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    private readonly CameraPreview _cameraPreview;
    private readonly ITextRecognizer _textRecognizer;
    private long _lastAnalyzedAt;

    public PlateAnalyzer(Context context, CameraPreview cameraPreview)
    {
        _cameraPreview = cameraPreview;
        _textRecognizer = TextRecognition.GetClient(TextRecognizerOptions.DefaultOptions);
    }

    public void Analyze(IImageProxy imageProxy)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - _lastAnalyzedAt < 750)
        {
            imageProxy.Close();
            return;
        }

        _lastAnalyzedAt = now;

        var mediaImage = imageProxy.Image;
        if (mediaImage is null)
        {
            imageProxy.Close();
            return;
        }

        var image = InputImage.FromMediaImage(mediaImage, imageProxy.ImageInfo.RotationDegrees);
        _textRecognizer.Process(image)
            .AddOnSuccessListener(new TextSuccessListener(_cameraPreview))
            .AddOnFailureListener(new TextFailureListener())
            .AddOnCompleteListener(new ImageCloseListener(imageProxy));
    }

    private sealed class TextSuccessListener : Java.Lang.Object, global::Android.Gms.Tasks.IOnSuccessListener
    {
        private readonly CameraPreview _cameraPreview;

        public TextSuccessListener(CameraPreview cameraPreview)
        {
            _cameraPreview = cameraPreview;
        }

        public void OnSuccess(Java.Lang.Object? result)
        {
            if (result is not Text text || string.IsNullOrWhiteSpace(text.GetText()))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => _cameraPreview.NotifyRecognizedText(text.GetText()));
        }
    }

    private sealed class TextFailureListener : Java.Lang.Object, global::Android.Gms.Tasks.IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception e)
        {
        }
    }

    private sealed class ImageCloseListener : Java.Lang.Object, global::Android.Gms.Tasks.IOnCompleteListener
    {
        private readonly IImageProxy _imageProxy;

        public ImageCloseListener(IImageProxy imageProxy)
        {
            _imageProxy = imageProxy;
        }

        public void OnComplete(global::Android.Gms.Tasks.Task task)
        {
            _imageProxy.Close();
        }
    }
}
