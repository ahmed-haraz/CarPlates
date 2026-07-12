using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Enums;
using CarPlates.Domain.ValueObjects;
using CarPlates.Shared.Constants;
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

    // Splits raw OCR/manual-entry text into individual candidate tokens.
    // ML Kit frequently returns the plate number and the plate letters as
    // separate lines/blocks (e.g. "395" on one line, "BTN" on the next), so
    // we must not rely on the whole string containing both digits and
    // letters together.
    private static readonly Regex TokenSplitter = new(@"[\s\-_,;/]+", RegexOptions.Compiled);

    private static readonly Regex ArabicLetters = new(@"^[\u0621-\u064A]{1,3}$", RegexOptions.Compiled);
    private static readonly Regex LatinLetters = new(@"^[A-Z]{1,3}$", RegexOptions.Compiled);
    private static readonly Regex Digits = new(@"^[0-9]{2,4}$", RegexOptions.Compiled);

    // Fallback pattern for text that already arrives as a single merged
    // token, e.g. manual entry of "395BTN" or "5984VJV".
    private static readonly Regex CombinedAlnum = new(@"\b([0-9]{2,4}[A-Z]{1,3}|[A-Z]{1,3}[0-9]{2,4})\b", RegexOptions.Compiled);

    private static readonly Regex EgyptianArabic = new(@"[\d\u0660-\u0669]{1,4}[\s-]*[\u0621-\u064A]{1,3}", RegexOptions.Compiled);

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

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new PlateRecognitionResult(false, null, text, 0, "No valid plate pattern found"));
        }

        var countryHint = DetectCountryHint(text);

        // 1) Preferred path: look at the text token-by-token (splitting on
        // whitespace/newlines/dashes) so plates whose number and letters were
        // read as separate lines are still combined correctly.
        var tokenResult = TryMatchFromTokens(text, countryHint);
        if (tokenResult != null)
        {
            return Task.FromResult(BuildSuccessResult(text, tokenResult.Value.Value, tokenResult.Value.Type, tokenResult.Value.Confidence));
        }

        // 2) Fallback: the text may already be a single merged token
        // (typical of manual entry, e.g. "395BTN" or "5984 VJV" typed as-is).
        var normalizedText = text.ToEnglishNumbers().NormalizePlate();

        var combinedMatch = CombinedAlnum.Match(normalizedText);
        if (combinedMatch.Success)
        {
            var type = countryHint switch
            {
                PlateType.Saudi => PlateType.Saudi,
                PlateType.Egyptian => PlateType.Egyptian,
                _ => InferTypeFromDigitCount(combinedMatch.Value)
            };
            return Task.FromResult(BuildSuccessResult(text, combinedMatch.Value, type, 0.85f));
        }

        // 3) Fallback: Egyptian plate typed/read fully in Arabic script.
        var egyptianMatch = EgyptianArabic.Match(normalizedText);
        if (egyptianMatch.Success)
        {
            return Task.FromResult(BuildSuccessResult(text, egyptianMatch.Value, PlateType.Egyptian, 0.85f));
        }

        _loggingService.LogOcr(text, null, 0.0f);
        return Task.FromResult(new PlateRecognitionResult(false, null, text, 0, "No valid plate pattern found"));
    }

    public bool IsValidPlate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (TryMatchFromTokens(text, DetectCountryHint(text)) != null) return true;

        var normalized = text.ToEnglishNumbers().NormalizePlate();
        return CombinedAlnum.IsMatch(normalized) || EgyptianArabic.IsMatch(normalized);
    }

    private (string Value, PlateType Type, float Confidence)? TryMatchFromTokens(string text, PlateType countryHint)
    {
        var rawTokens = TokenSplitter.Split(text)
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .Select(t => t.ToEnglishNumbers())
            .ToList();

        string? digitsToken = null;
        string? latinLettersToken = null;
        string? arabicLettersToken = null;

        foreach (var rawToken in rawTokens)
        {
            var token = rawToken.ToUpperInvariant();

            if (IsNoiseWord(token)) continue;

            if (digitsToken is null && Digits.IsMatch(token))
            {
                digitsToken = token;
                continue;
            }

            if (latinLettersToken is null && LatinLetters.IsMatch(token))
            {
                latinLettersToken = token;
                continue;
            }

            if (arabicLettersToken is null && ArabicLetters.IsMatch(rawToken))
            {
                arabicLettersToken = rawToken;
            }
        }

        if (digitsToken is null) return null;

        if (latinLettersToken is not null)
        {
            var type = countryHint switch
            {
                PlateType.Saudi => PlateType.Saudi,
                PlateType.Egyptian => PlateType.Egyptian,
                _ => InferTypeFromDigitCount(digitsToken)
            };
            var confidence = countryHint == PlateType.Unknown ? 0.85f : 0.9f;
            return ($"{digitsToken} {latinLettersToken}", type, confidence);
        }

        if (arabicLettersToken is not null)
        {
            return ($"{digitsToken} {arabicLettersToken}", PlateType.Egyptian, 0.85f);
        }

        return null;
    }

    private static PlateType InferTypeFromDigitCount(string valueContainingDigits)
    {
        var digitCount = valueContainingDigits.Count(char.IsDigit);
        // Saudi private plates use 4 digits, Egyptian private plates use up to 3-4;
        // this is only a best-effort guess used when no country label was read.
        return digitCount >= 4 ? PlateType.Saudi : PlateType.Egyptian;
    }

    private static bool IsNoiseWord(string token)
    {
        return ScannerConstants.OcrNoiseWords.Any(noise =>
            string.Equals(noise, token, StringComparison.OrdinalIgnoreCase));
    }

    private static PlateType DetectCountryHint(string text)
    {
        if (ContainsAny(text, "KSA", "SAUDI", "ARABIA", "KINGDOM", "السعودية", "المملكة", "السعوديه"))
        {
            return PlateType.Saudi;
        }

        if (ContainsAny(text, "EGYPT", "مصر", "جمهورية"))
        {
            return PlateType.Egyptian;
        }

        return PlateType.Unknown;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    private PlateRecognitionResult BuildSuccessResult(string rawText, string value, PlateType type, float confidence)
    {
        _loggingService.LogOcr(rawText, value, confidence);
        return new PlateRecognitionResult(
            true,
            PlateNumber.Create(value, type, confidence),
            rawText,
            confidence,
            null);
    }
}
