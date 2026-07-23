using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace CarPlates.API.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{
    private readonly IConfiguration _configuration = configuration;

    public string GenerateAccessToken(ApplicationUser user)
    {
        var secret = Environment.GetEnvironmentVariable("JWT__Key")
    ?? throw new InvalidOperationException("JWT__Key is missing.");

        var issuer = Environment.GetEnvironmentVariable("JWT__Issuer")
            ?? throw new InvalidOperationException("JWT__Issuer is missing.");

        var audience = Environment.GetEnvironmentVariable("JWT__Audience")
            ?? throw new InvalidOperationException("JWT__Audience is missing.");

        if (!int.TryParse(Environment.GetEnvironmentVariable("JWT__ExpiryMinutes"), out var expiryMinutes))
        {
            throw new InvalidOperationException("JWT__ExpiryMinutes is missing or invalid.");
        }

        var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new(JwtRegisteredClaimNames.Name, user.UserName ?? ""),
                new("fullName", user.FullName),
                new("branchId", user.BranchId.ToString()),
                new("salesRepId", user.SalesRepId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        // In production, validate against stored refresh tokens in DB
        // For now, just decode to get user info
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(refreshToken);
            return new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims));
        }
        catch
        {
            return null;
        }
    }
}
