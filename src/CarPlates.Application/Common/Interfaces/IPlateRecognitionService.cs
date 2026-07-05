using CarPlates.Domain.ValueObjects;

namespace CarPlates.Application.Common.Interfaces;

public interface IPlateRecognitionService
{
    Task<PlateRecognitionResult> RecognizeAsync(Stream imageStream, CancellationToken cancellationToken = default);
    Task<PlateRecognitionResult> RecognizeFromTextAsync(string text, CancellationToken cancellationToken = default);
    bool IsValidPlate(string text);
}

public record PlateRecognitionResult(
    bool Success,
    PlateNumber? PlateNumber,
    string? RawText,
    float Confidence,
    string? ErrorMessage);
