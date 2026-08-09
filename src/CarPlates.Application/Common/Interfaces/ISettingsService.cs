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
    Task<string> GetCompanyCodeAsync();
    Task SetCompanyCodeAsync(string companyCode);
    Task<string> GetCompanyNameAsync();
    Task SetCompanyNameAsync(string companyName);
    Task<string> GetCompanyLogoUrlAsync();
    Task SetCompanyLogoUrlAsync(string logoUrl);
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
    string CompanyCode,
    string CompanyName,
    string CompanyLogoUrl,
    float OcrConfidence,
    bool AutoResume,
    bool NotificationsEnabled);
