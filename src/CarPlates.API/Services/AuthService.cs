using CarPlates.API.Interface;
using CarPlates.API.Models;
using CarPlates.API.Models.DTOs;
using Microsoft.AspNetCore.Identity;

namespace CarPlates.API.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtService jwtService) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IJwtService _jwtService = jwtService;

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null || !user.IsActive) return null;

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Store refresh token (in production, save to DB)
        user.SecurityStamp = refreshToken;
        await _userManager.UpdateAsync(user);

        return new LoginResponseDto(
            accessToken,
            refreshToken,
            new UserDto(user.Id, user.UserName!, user.Email!, user.FullName, user.ProfilePhotoUrl, roles.FirstOrDefault() ?? "Operator"));
    }

    public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        // Validate refresh token logic here
        // For demo, simplified
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
            await _userManager.AddToRoleAsync(user, "Operator");
        }
        return result.Succeeded;
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.UserName!, user.Email!, user.FullName, user.ProfilePhotoUrl, roles.FirstOrDefault() ?? "Operator");
    }
}
