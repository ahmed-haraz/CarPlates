using System.Security.Cryptography;
using System.Text;
using CarPlates.API.Configuration;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarPlates.API.Services;

public class AuthService(
    ApplicationDbContext context,
    IJwtService jwtService,
    IOptions<LegacyDesOptions> desOptions) : IAuthService
{

    private readonly ApplicationDbContext _context = context;
    private readonly IJwtService _jwtService = jwtService;
    private readonly LegacyDesOptions _desOptions = desOptions.Value;

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.FwUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == request.Username);

        if (user == null || !PasswordMatches(user.Password!, request.Password))
        {
            return null;
        }

        var applicationUser = new ApplicationUser
        {
            Id = user.ID.ToString(),
            UserName = user.UserName,
            Email = user.email,
            FullName = GetFullName(user),
            IsActive = true,
            BranchId = user.BranchID ?? 0,
            SalesRepId = user.SalesRepID ?? 0
        };


        var accessToken = _jwtService.GenerateAccessToken(applicationUser);
        var refreshToken = _jwtService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshTokens
        {
            UserId = applicationUser.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Revoked = false
        });

        await _context.SaveChangesAsync();

        return new LoginResponseDto(
            accessToken,
            refreshToken,
            new UserDto(
                applicationUser.Id,
                applicationUser.UserName!,
                applicationUser.Email!,
                applicationUser.FullName,
                user.BranchID ?? 0,
                user.CashBoxID ?? 0,
                user.CarID ?? 0,
                user.StoreID ?? 0,
                user.SalesRepID ?? 0,
                user.UserType ?? 0
            ));
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x =>
                x.Token == request.RefreshToken &&
                !x.Revoked &&
                x.ExpiresAt > DateTime.UtcNow);

        if (storedToken == null)
        {
            return null;
        }

        var user = await _context.FwUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID.ToString() == storedToken.UserId);

        if (user == null)
        {
            return null;
        }

        var applicationUser = new ApplicationUser
        {
            Id = user.ID.ToString(),
            UserName = user.UserName,
            Email = user.email,
            FullName = GetFullName(user),
            IsActive = true,
            BranchId = user.BranchID ?? 0,
            SalesRepId = user.SalesRepID ?? 0
        };


        var newAccessToken = _jwtService.GenerateAccessToken(applicationUser);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Revoke the old refresh token
        storedToken.Revoked = true;

        // Save the new refresh token
        _context.RefreshTokens.Add(new RefreshTokens
        {
            UserId = applicationUser.Id,
            Token = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Revoked = false
        });

        await _context.SaveChangesAsync();

        return new LoginResponseDto(
            newAccessToken,
            newRefreshToken,
            new UserDto(
                applicationUser.Id,
                applicationUser.UserName!,
                applicationUser.Email!,
                applicationUser.FullName,
                user.BranchID ?? 0,
                user.CashBoxID ?? 0,
                user.CarID ?? 0,
                user.StoreID ?? 0,
                user.SalesRepID ?? 0,
                user.UserType ?? 0
            ));
    }

    public Task<bool> RegisterAsync(RegisterRequestDto request)
    {
        throw new NotSupportedException(
            "User registration is managed by the legacy system.");
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
            user.UserName!,
            user.email!,
            GetFullName(user),
            user.BranchID ?? 0,
            user.CashBoxID ?? 0,
            user.CarID ?? 0,
            user.StoreID ?? 0,
            user.SalesRepID ?? 0,
            user.UserType ?? 0);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken && !x.Revoked);

        if (storedToken == null)
        {
            // Already revoked/unknown - treat logout as a no-op success so the
            // client can always clear its local session state.
            return true;
        }

        storedToken.Revoked = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private bool PasswordMatches(string storedPassword, string plainPassword)
    {
        if (string.IsNullOrEmpty(storedPassword))
        {
            return false;
        }

        return string.Equals(
            storedPassword,
            LegacyDesEncrypt(plainPassword),
            StringComparison.Ordinal);
    }

    private string LegacyDesEncrypt(string plain)
    {
        try
        {
            var desKey = Encoding.UTF8.GetString(
                Convert.FromBase64String(_desOptions.Key));

            byte[] bKey = Encoding.UTF8.GetBytes(desKey.Substring(0, 8));
            byte[] desIv = Convert.FromBase64String(_desOptions.Iv);

#pragma warning disable SYSLIB0021
            using var des = new DESCryptoServiceProvider();

            byte[] inputArray = Encoding.UTF8.GetBytes(plain);

            using var ms = new MemoryStream();

            using var cs = new CryptoStream(
                ms,
                des.CreateEncryptor(bKey, desIv),
                CryptoStreamMode.Write);

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

        return user.UserName!;
    }
}