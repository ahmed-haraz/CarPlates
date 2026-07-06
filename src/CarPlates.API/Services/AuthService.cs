using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using CarPlates.API.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class AuthService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService) : IAuthService
{
    private const string DefaultRole = "Operator";

    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IJwtService _jwtService = jwtService;

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.FwUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == request.Username, CancellationToken.None);

        if (user == null || user.Password != request.Password)
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
