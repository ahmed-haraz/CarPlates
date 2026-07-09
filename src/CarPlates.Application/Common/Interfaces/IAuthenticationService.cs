using CarPlates.Application.Common.DTOs;

namespace CarPlates.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);

}

public record AuthResult(bool Success, string? AccessToken, string? RefreshToken, string? ErrorMessage, UserDto info);
