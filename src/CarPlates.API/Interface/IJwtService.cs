using CarPlates.API.Models;
using System.Security.Claims;

namespace CarPlates.API.Interface;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}