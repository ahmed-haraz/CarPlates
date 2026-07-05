namespace CarPlates.Application.Common.Interfaces;

public interface ILoggingService
{
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogScanner(string plateNumber, float confidence, bool success);
    void LogOcr(string rawText, string? normalizedText, float confidence);
    void LogApi(string endpoint, bool success, long responseTimeMs);
}
