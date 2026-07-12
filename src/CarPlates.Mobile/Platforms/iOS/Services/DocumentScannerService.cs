using CarPlates.Application.Common.Interfaces;

namespace CarPlates.Mobile.Platforms.iOS.Services;

/// <summary>
/// Placeholder - the tap-to-scan document scanner flow is implemented for
/// Android only in this change (via ML Kit's GmsDocumentScanning). Wire up
/// VisionKit's VNDocumentCameraViewController + Vision text recognition here
/// to support iOS.
/// </summary>
public class DocumentScannerService : IDocumentScannerService
{
    public Task<DocumentScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DocumentScanResult(
            false, null, null, "Tap-to-scan is not yet implemented on iOS. Use manual entry."));
    }
}
