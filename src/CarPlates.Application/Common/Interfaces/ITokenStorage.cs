namespace CarPlates.Application.Common.Interfaces;

public interface ITokenStorage
{
    Task SaveTokenAsync(string accessToken, string refreshToken);
    Task<(string? AccessToken, string? RefreshToken)> GetTokensAsync();
    Task ClearTokensAsync();
    Task<bool> HasValidTokenAsync();
}
