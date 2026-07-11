namespace CarPlates.Application.Common.Interfaces;

public record ConnectivityResult(bool IsReachable, string? ErrorMessage);

/// <summary>Lets Settings verify the configured API URL is actually reachable,
/// without needing to be logged in first (hits the unauthenticated health endpoint).</summary>
public interface IApiConnectivityService
{
    Task<ConnectivityResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}
