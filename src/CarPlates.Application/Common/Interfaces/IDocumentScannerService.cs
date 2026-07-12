namespace CarPlates.Application.Common.Interfaces;

/// <summary>
/// Launches a native "tap to scan" document-scanning UI (crop/enhance a
/// single photo of a plate) and runs OCR on the captured page, returning
/// the raw recognized text so it can be parsed by <see cref="IPlateRecognitionService"/>.
/// This is the counterpart to the continuous, automatic frame analysis done
/// while the live camera preview is running.
/// </summary>
public interface IDocumentScannerService
{
    Task<DocumentScanResult> ScanAsync(CancellationToken cancellationToken = default);
}

public record DocumentScanResult(
    bool Success,
    string? RecognizedText,
    string? ImagePath,
    string? ErrorMessage);
