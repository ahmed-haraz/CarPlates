using CarPlates.Application.Common.Interfaces;
using CarPlates.Domain.Enums;
using CarPlates.Shared.Constants;
using Microsoft.Maui.Storage;

namespace CarPlates.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    public async Task<AppSettings> GetSettingsAsync()
    {
        return new AppSettings(
            await GetThemeAsync(),
            await GetLanguageAsync(),
            await GetApiUrlAsync(),
            await GetOcrConfidenceAsync(),
            await GetAutoResumeAsync(),
            await GetNotificationsEnabledAsync());
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await SetThemeAsync(settings.Theme);
        await SetLanguageAsync(settings.Language);
        await SetApiUrlAsync(settings.ApiUrl);
        await SetOcrConfidenceAsync(settings.OcrConfidence);
        await SetAutoResumeAsync(settings.AutoResume);
        await SetNotificationsEnabledAsync(settings.NotificationsEnabled);
    }

    public Task<AppTheme> GetThemeAsync()
    {
        var theme = Preferences.Get("theme", "System");
        return Task.FromResult(Enum.TryParse<AppTheme>(theme, out var result) ? result : AppTheme.System);
    }

    public Task SetThemeAsync(AppTheme theme)
    {
        Preferences.Set("theme", theme.ToString());
        return Task.CompletedTask;
    }

    public Task<string> GetLanguageAsync()
    {
        return Task.FromResult(Preferences.Get("language", "en"));
    }

    public Task SetLanguageAsync(string language)
    {
        Preferences.Set("language", language);
        return Task.CompletedTask;
    }

    public Task<string> GetApiUrlAsync()
    {
        return Task.FromResult(Preferences.Get("api_url", ApiConstants.DefaultApiUrl));
    }

    public Task SetApiUrlAsync(string url)
    {
        Preferences.Set("api_url", url);
        return Task.CompletedTask;
    }

    public Task<float> GetOcrConfidenceAsync()
    {
        return Task.FromResult(Preferences.Get("ocr_confidence", ScannerConstants.DefaultOcrConfidence));
    }

    public Task SetOcrConfidenceAsync(float confidence)
    {
        Preferences.Set("ocr_confidence", confidence);
        return Task.CompletedTask;
    }

    public Task<bool> GetAutoResumeAsync()
    {
        return Task.FromResult(Preferences.Get("auto_resume", true));
    }

    public Task SetAutoResumeAsync(bool autoResume)
    {
        Preferences.Set("auto_resume", autoResume);
        return Task.CompletedTask;
    }

    public Task<bool> GetNotificationsEnabledAsync()
    {
        return Task.FromResult(Preferences.Get("notifications_enabled", true));
    }

    public Task SetNotificationsEnabledAsync(bool enabled)
    {
        Preferences.Set("notifications_enabled", enabled);
        return Task.CompletedTask;
    }
}
