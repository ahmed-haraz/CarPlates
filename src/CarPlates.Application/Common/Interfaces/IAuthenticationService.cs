namespace CarPlates.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public record AuthResult(bool Success, string? AccessToken, string? RefreshToken, string? ErrorMessage);
public record UserInfo(string Username, string Email, string FullName, string? ProfilePhotoUrl, string Role);
