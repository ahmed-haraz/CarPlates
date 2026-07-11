namespace CarPlates.Mobile.Controls;

public partial class CameraPreview : ContentView
{
    public static readonly BindableProperty IsTorchOnProperty = BindableProperty.Create(
        nameof(IsTorchOn), typeof(bool), typeof(CameraPreview), false);

    public static readonly BindableProperty CameraFacingProperty = BindableProperty.Create(
        nameof(CameraFacing), typeof(CameraFacing), typeof(CameraPreview), CameraFacing.Back);

    public static readonly BindableProperty IsPreviewingProperty = BindableProperty.Create(
        nameof(IsPreviewing), typeof(bool), typeof(CameraPreview), false);

    public bool IsTorchOn
    {
        get => (bool)GetValue(IsTorchOnProperty);
        set => SetValue(IsTorchOnProperty, value);
    }

    public CameraFacing CameraFacing
    {
        get => (CameraFacing)GetValue(CameraFacingProperty);
        set => SetValue(CameraFacingProperty, value);
    }

    public bool IsPreviewing
    {
        get => (bool)GetValue(IsPreviewingProperty);
        set => SetValue(IsPreviewingProperty, value);
    }

    public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;

    public CameraPreview()
    {
        InitializeComponent();
    }

    public void StartPreview()
    {
        IsPreviewing = true;
        OnStartPreview();
    }

    public void StopPreview()
    {
        IsPreviewing = false;
        OnStopPreview();
    }

    protected virtual void OnStartPreview() { }
    protected virtual void OnStopPreview() { }
    protected virtual void OnFrameCaptured(FrameCapturedEventArgs e) => FrameCaptured?.Invoke(this, e);
}

public enum CameraFacing
{
    Back,
    Front
}

public class FrameCapturedEventArgs : EventArgs
{
    public byte[] ImageData { get; }
    public int Width { get; }
    public int Height { get; }

    public FrameCapturedEventArgs(byte[] imageData, int width, int height)
    {
        ImageData = imageData;
        Width = width;
        Height = height;
    }
}
