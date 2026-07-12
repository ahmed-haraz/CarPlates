using Android.App;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using CarPlates.Application.Common.Interfaces;
using Net.Google.MLKit.Vision.DocumentScanner;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Latin;
using Droid = Android;

namespace CarPlates.Mobile.Platforms.Android.Services;


public class DocumentScannerService : Java.Lang.Object,
    IDocumentScannerService,
    Droid.Gms.Tasks.IOnSuccessListener,
    Droid.Gms.Tasks.IOnFailureListener,
    IActivityResultCallback
{
    private readonly ITextRecognizer _textRecognizer;
    private readonly ActivityResultLauncher? _scannerLauncher;

    private TaskCompletionSource<DocumentScanResult>? _pendingScan;

    public DocumentScannerService()
    {
        _textRecognizer = TextRecognition.GetClient(TextRecognizerOptions.DefaultOptions);

        if (Platform.CurrentActivity is AndroidX.Activity.ComponentActivity activity)
        {
            _scannerLauncher = activity.RegisterForActivityResult(
                new ActivityResultContracts.StartIntentSenderForResult(), this);
        }
    }

    public Task<DocumentScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<DocumentScanResult>();
        _pendingScan = completion;

        if (_scannerLauncher is null)
        {
            completion.TrySetResult(new DocumentScanResult(
                false, null, null, "The document scanner is not available on this screen."));
            return completion.Task;
        }

        try
        {
            var options = new GmsDocumentScannerOptions.Builder()
                .SetGalleryImportAllowed(false)
                .SetPageLimit(1)
                .SetResultFormats(GmsDocumentScannerOptions.ResultFormatJpeg)
                .SetScannerMode(GmsDocumentScannerOptions.ScannerModeFull)
                .Build();

            var scanner = GmsDocumentScanning.GetClient(options);
            var intentSender = scanner.GetStartScanIntent(Platform.CurrentActivity);
            intentSender.AddOnSuccessListener(this);
            intentSender.AddOnFailureListener(this);
        }
        catch (Exception ex)
        {
            completion.TrySetResult(new DocumentScanResult(false, null, null, ex.Message));
        }

        return completion.Task;
    }

    public void OnSuccess(Java.Lang.Object? result)
    {
        if (result is Droid.Content.IntentSender intentSender && _scannerLauncher is not null)
        {
            _scannerLauncher.Launch(new IntentSenderRequest.Builder(intentSender).Build());
        }
        else
        {
            _pendingScan?.TrySetResult(new DocumentScanResult(false, null, null, "The scanner could not be launched."));
        }
    }

    public void OnFailure(Java.Lang.Exception e)
    {
        _pendingScan?.TrySetResult(new DocumentScanResult(false, null, null, e.Message));
    }

    public void OnActivityResult(Java.Lang.Object? result)
    {
        var completion = _pendingScan;
        if (completion is null) return;

        if (result is not ActivityResult activityResult)
        {
            completion.TrySetResult(new DocumentScanResult(false, null, null, "No result from the scanner."));
            return;
        }

        if (activityResult.ResultCode != (int)Result.Ok)
        {
            // User backed out of the scanner - not an error, just no capture.
            completion.TrySetResult(new DocumentScanResult(false, null, null, null));
            return;
        }

        var scanResult = GmsDocumentScanningResult.FromActivityResultIntent(activityResult.Data);
        var page = scanResult?.Pages?.FirstOrDefault();

        if (page?.ImageUri is null)
        {
            completion.TrySetResult(new DocumentScanResult(false, null, null, "No page was captured."));
            return;
        }

        RunTextRecognition(page.ImageUri, completion);
    }

    private void RunTextRecognition(Droid.Net.Uri imageUri, TaskCompletionSource<DocumentScanResult> completion)
    {
        try
        {
            var image = InputImage.FromFilePath(global::Android.App.Application.Context, imageUri);
            _textRecognizer.Process(image)
                .AddOnSuccessListener(new OcrSuccessListener(completion, imageUri.Path))
                .AddOnFailureListener(new OcrFailureListener(completion, imageUri.Path));
        }
        catch (Exception ex)
        {
            completion.TrySetResult(new DocumentScanResult(false, null, imageUri.Path, ex.Message));
        }
    }

    private sealed class OcrSuccessListener(TaskCompletionSource<DocumentScanResult> completion, string? imagePath) : Java.Lang.Object, Droid.Gms.Tasks.IOnSuccessListener
    {
        private readonly TaskCompletionSource<DocumentScanResult> _completion = completion;
        private readonly string? _imagePath = imagePath;

        public void OnSuccess(Java.Lang.Object? result)
        {
            var recognizedText = (result as Text)?.GetText() ?? string.Empty;

            _completion.TrySetResult(new DocumentScanResult(
                Success: !string.IsNullOrWhiteSpace(recognizedText),
                RecognizedText: recognizedText,
                ImagePath: _imagePath,
                ErrorMessage: string.IsNullOrWhiteSpace(recognizedText)
                    ? "No text was detected on the scanned image."
                    : null));
        }
    }

    private sealed class OcrFailureListener(TaskCompletionSource<DocumentScanResult> completion, string? imagePath) : Java.Lang.Object, Droid.Gms.Tasks.IOnFailureListener
    {
        private readonly TaskCompletionSource<DocumentScanResult> _completion = completion;
        private readonly string? _imagePath = imagePath;

        public void OnFailure(Java.Lang.Exception e)
        {
            _completion.TrySetResult(new DocumentScanResult(false, null, _imagePath, e.Message));
        }
    }
}
