using CarPlates.Application.Common.Interfaces;
using Serilog;

namespace CarPlates.Infrastructure.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogger _logger;

    public LoggingService()
    {
        _logger = Log.ForContext<LoggingService>();
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.Error(exception, message, args);
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    public void LogScanner(string plateNumber, float confidence, bool success)
    {
        _logger.Information(
            "Scanner: Plate={PlateNumber}, Confidence={Confidence}, Success={Success}",
            plateNumber, confidence, success);
    }

    public void LogOcr(string rawText, string? normalizedText, float confidence)
    {
        _logger.Information(
            "OCR: RawText={RawText}, Normalized={Normalized}, Confidence={Confidence}",
            rawText, normalizedText ?? "N/A", confidence);
    }

    public void LogApi(string endpoint, bool success, long responseTimeMs)
    {
        _logger.Information(
            "API: Endpoint={Endpoint}, Success={Success}, ResponseTime={ResponseTimeMs}ms",
            endpoint, success, responseTimeMs);
    }
}
