namespace CarPlates.Application.Common.Interfaces;

public interface ICameraService
{
    Task<CameraResult> CaptureFrameAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleTorchAsync();
    Task<bool> SwitchCameraAsync();
    Task StartPreviewAsync();
    Task StopPreviewAsync();
    event EventHandler<FrameCapturedEventArgs>? FrameCaptured;
}

public record CameraResult(bool Success, byte[]? ImageData, string? ErrorMessage);
public record FrameCapturedEventArgs(byte[] ImageData, int Width, int Height);
