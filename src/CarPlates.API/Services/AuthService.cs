using System.Security.Cryptography;
using System.Text;
using CarPlates.API.Configuration;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using CarPlates.API.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarPlates.API.Services;

public class AuthService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    IOptions<LegacyDesOptions> desOptions) : IAuthService
{
    private const string DefaultRole = "Operator";

    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtService _jwtService = jwtService;
    private readonly LegacyDesOptions _desOptions = desOptions.Value;

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.FwUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == request.Username, CancellationToken.None);

        if (user == null || !PasswordMatches(user.Password, request.Password))
        {
            return null;
        }

        var applicationUser = new ApplicationUser
        {
            Id = user.ID.ToString(),
            UserName = user.UserName,
            Email = user.email,
            FullName = GetFullName(user),
            IsActive = true
        };

        var roles = new List<string> { DefaultRole };
        var accessToken = _jwtService.GenerateAccessToken(applicationUser, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new LoginResponseDto(
            accessToken,
            refreshToken,
            new UserDto(
                applicationUser.Id,
                applicationUser.UserName,
                applicationUser.Email,
                applicationUser.FullName,
                applicationUser.ProfilePhotoUrl,
                DefaultRole));
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        // Validate refresh token logic here
        // For demo, simplified
        await Task.CompletedTask;
        return null;
    }

    public async Task<bool> RegisterAsync(RegisterRequestDto request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, DefaultRole);
        }
        return result.Succeeded;
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _context.FwUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ID.ToString() == userId);

        if (user == null)
        {
            return null;
        }

        return new UserDto(
            user.ID.ToString(),
            user.UserName,
            user.email,
            GetFullName(user),
            null,
            DefaultRole);
    }


    private bool PasswordMatches(string storedPassword, string plainPassword)
    {
        if (string.IsNullOrEmpty(storedPassword))
        {
            return false;
        }

        return string.Equals(storedPassword, LegacyDesEncrypt(plainPassword), StringComparison.Ordinal);
    }

    private string LegacyDesEncrypt(string plain)
    {
        try
        {
            // "9&%$#@!12*ABxyZ".Substring(0, 8) → "9&%$#@!1"
            var desKey = Encoding.UTF8.GetString(Convert.FromBase64String(_desOptions.Key));
            byte[] bKey = Encoding.UTF8.GetBytes(desKey.Substring(0, 8));
            byte[] desIv = Convert.FromBase64String(_desOptions.Iv);

#pragma warning disable SYSLIB0021
            using var des = new DESCryptoServiceProvider();
            byte[] inputArray = Encoding.UTF8.GetBytes(plain);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, des.CreateEncryptor(bKey, desIv), CryptoStreamMode.Write);
            cs.Write(inputArray, 0, inputArray.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
#pragma warning restore SYSLIB0021
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetFullName(fw_Users user)
    {
        if (!string.IsNullOrWhiteSpace(user.UserFullName_En))
        {
            return user.UserFullName_En;
        }

        if (!string.IsNullOrWhiteSpace(user.UserFullName_Ar))
        {
            return user.UserFullName_Ar;
        }

        return user.UserName;
    }
}
