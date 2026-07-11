using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Enums;
using CarPlates.Domain.ValueObjects;
using CarPlates.Shared.Extensions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CarPlates.Infrastructure.OCR;

public class PlateRecognitionService(
    ILoggingService loggingService,
    ILogger<PlateRecognitionService> logger) : IPlateRecognitionService
{
    private readonly ILoggingService _loggingService = loggingService;
    private readonly ILogger<PlateRecognitionService> _logger = logger;

    public Task<PlateRecognitionResult> RecognizeAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        // This is a placeholder - actual implementation uses ML Kit OCR
        // The real implementation would process the image stream through ML Kit
        _logger.LogWarning("RecognizeAsync with Stream is a placeholder - use platform-specific implementation");
        return Task.FromResult(new PlateRecognitionResult(false, null, null, 0, "Use platform-specific OCR implementation"));
    }

    public Task<PlateRecognitionResult> RecognizeFromTextAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Recognizing plate from text: {Text}", text);

        var normalizedText = text.ToEnglishNumbers().NormalizePlate();

        // Try Egyptian plate pattern first
        var egyptianMatch = TryMatchEgyptianPlate(normalizedText);
        if (egyptianMatch != null)
        {
            _loggingService.LogOcr(text, egyptianMatch, 0.85f);
            return Task.FromResult(new PlateRecognitionResult(
                true,
                PlateNumber.Create(egyptianMatch, PlateType.Egyptian, 0.85f),
                text,
                0.85f,
                null));
        }

        // Try English plate pattern
        var englishMatch = TryMatchEnglishPlate(normalizedText);
        if (englishMatch != null)
        {
            _loggingService.LogOcr(text, englishMatch, 0.90f);
            return Task.FromResult(new PlateRecognitionResult(
                true,
                PlateNumber.Create(englishMatch, PlateType.English, 0.90f),
                text,
                0.90f,
                null));
        }

        _loggingService.LogOcr(text, null, 0.0f);
        return Task.FromResult(new PlateRecognitionResult(false, null, text, 0, "No valid plate pattern found"));
    }

    public bool IsValidPlate(string text)
    {
        var normalized = text.ToEnglishNumbers().NormalizePlate();
        return TryMatchEgyptianPlate(normalized) != null || TryMatchEnglishPlate(normalized) != null;
    }

    private string? TryMatchEgyptianPlate(string text)
    {
        // Egyptian plates: 1234 ABC (numbers + Arabic letters)
        // Or Arabic numerals: ١٢٣٤ أ ب ج
        var pattern = @"[\d٠-٩]{1,4}[\s-]*[ء-ي]{1,3}";
        var match = Regex.Match(text, pattern);
        return match.Success ? match.Value : null;
    }

    private string? TryMatchEnglishPlate(string text)
    {
        // English/Saudi plate OCR commonly arrives as separate number and letter
        // groups (for example, "395 BTN") or as one normalized token. Skip
        // country/header words such as EGYPT by evaluating every candidate.
        var pattern = @"[A-Z0-9]{3,10}";
        foreach (Match match in Regex.Matches(text, pattern))
        {
            var value = match.Value;
            if (value.Any(char.IsLetter) && value.Any(char.IsDigit))
            {
                return value;
            }
        }

        return null;
    }
}
