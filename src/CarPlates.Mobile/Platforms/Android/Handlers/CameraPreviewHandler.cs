using Android.Content;
using Android.Content.PM;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using CarPlates.Mobile.Controls;
using Microsoft.Maui.Handlers;

namespace CarPlates.Mobile.Platforms.Android.Handlers;

public partial class CameraPreviewHandler : ViewHandler<CameraPreview, PreviewView>
{
    private ICamera? _camera;
    private ProcessCameraProvider? _cameraProvider;

    public CameraPreviewHandler() : base(PropertyMapper)
    {
    }

    public static IPropertyMapper<CameraPreview, CameraPreviewHandler> PropertyMapper =
        new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(CameraPreview.IsPreviewing)] = MapIsPreviewing,
            [nameof(CameraPreview.CameraFacing)] = MapCameraFacing,
            [nameof(CameraPreview.IsTorchOn)] = MapTorch
        };

    protected override PreviewView CreatePlatformView()
    {
        var previewView = new PreviewView(Context);
        previewView.SetImplementationMode(PreviewView.ImplementationMode.Compatible);
        return previewView;
    }

    protected override void ConnectHandler(PreviewView platformView)
    {
        base.ConnectHandler(platformView);

        if (VirtualView.IsPreviewing)
        {
            _ = StartCameraAsync();
        }
    }

    protected override void DisconnectHandler(PreviewView platformView)
    {
        StopCamera();
        base.DisconnectHandler(platformView);
    }

    private static void MapIsPreviewing(CameraPreviewHandler handler, CameraPreview view)
    {
        if (view.IsPreviewing)
        {
            _ = handler.StartCameraAsync();
        }
        else
        {
            handler.StopCamera();
        }
    }

    private static void MapCameraFacing(CameraPreviewHandler handler, CameraPreview view)
    {
        if (view.IsPreviewing)
        {
            _ = handler.StartCameraAsync();
        }
    }

    private static void MapTorch(CameraPreviewHandler handler, CameraPreview view)
    {
        if (handler._camera?.CameraInfo.HasFlashUnit == true)
        {
            handler._camera.CameraControl.EnableTorch(view.IsTorchOn);
        }
    }

    private async Task StartCameraAsync()
    {
        try
        {
            if (ContextCompat.CheckSelfPermission(Context, global::Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                System.Diagnostics.Debug.WriteLine("[CameraPreviewHandler] Camera permission not granted, aborting start.");
                return;
            }

            var lifecycleOwner = Context as ILifecycleOwner ?? Platform.CurrentActivity as ILifecycleOwner;
            if (lifecycleOwner is null || PlatformView is null || VirtualView is null)
            {
                System.Diagnostics.Debug.WriteLine("[CameraPreviewHandler] Missing lifecycle owner, platform view, or virtual view.");
                return;
            }

            var providerFuture = ProcessCameraProvider.GetInstance(Context);
            _cameraProvider = (ProcessCameraProvider)await Task.Run(() => providerFuture.Get());
            _cameraProvider.UnbindAll();

            var preview = new Preview.Builder().Build();
            preview.SetSurfaceProvider(ContextCompat.GetMainExecutor(Context), PlatformView.SurfaceProvider);

            var analysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .Build();
            analysis.SetAnalyzer(ContextCompat.GetMainExecutor(Context), new PlateAnalyzer(Context, VirtualView));

            var selector = new CameraSelector.Builder()
                .RequireLensFacing(VirtualView.CameraFacing == CameraFacing.Front
                    ? CameraSelector.LensFacingFront
                    : CameraSelector.LensFacingBack)
                .Build();

            _camera = _cameraProvider.BindToLifecycle(lifecycleOwner, selector, preview, analysis);

            if (_camera.CameraInfo.HasFlashUnit)
            {
                _camera.CameraControl.EnableTorch(VirtualView.IsTorchOn);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CameraPreviewHandler] Failed to start camera: {ex}");
        }
    }

    private void StopCamera()
    {
        _cameraProvider?.UnbindAll();
        _camera = null;
    }
}
