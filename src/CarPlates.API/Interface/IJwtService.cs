using CarPlates.API.Models;
using System.Security.Claims;

namespace CarPlates.API.Interface;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}