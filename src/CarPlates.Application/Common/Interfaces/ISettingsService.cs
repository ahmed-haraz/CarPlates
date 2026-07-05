using CarPlates.Domain.Enums;

namespace CarPlates.Application.Common.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task<AppTheme> GetThemeAsync();
    Task SetThemeAsync(AppTheme theme);
    Task<string> GetLanguageAsync();
    Task SetLanguageAsync(string language);
    Task<string> GetApiUrlAsync();
    Task SetApiUrlAsync(string url);
    Task<float> GetOcrConfidenceAsync();
    Task SetOcrConfidenceAsync(float confidence);
    Task<bool> GetAutoResumeAsync();
    Task SetAutoResumeAsync(bool autoResume);
    Task<bool> GetNotificationsEnabledAsync();
    Task SetNotificationsEnabledAsync(bool enabled);
}

public record AppSettings(
    AppTheme Theme,
    string Language,
    string ApiUrl,
    float OcrConfidence,
    bool AutoResume,
    bool NotificationsEnabled);
