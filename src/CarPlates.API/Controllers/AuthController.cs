using System.Security.Claims;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<ActionResult> Login([FromBody] LoginRequestDto request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var result = await _authService.LoginAsync(request);
        if (result == null)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        if (!result.IsSuccess)
        {
            if (result.DeviceBlocked)
                return StatusCode(403, new { message = result.ErrorMessage, deviceBlocked = true });
            return Unauthorized(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (result == null) return Unauthorized(new { Message = "Invalid refresh token" });
        return Ok(result);
    }

    // Enrollment is disabled until an approved tenant-admin or invitation flow
    // binds tenant, role and branch server-side. Authentication alone is not admin.
    [HttpPost("register")]
    [Authorize]
    public ActionResult Register([FromBody] RegisterRequestDto request)
    {
        return StatusCode(StatusCodes.Status403Forbidden,
            new { Message = "Self-service enrollment is disabled." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserAsync(userId);
        if (user == null) return NotFound();

        return Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] LogoutRequestDto request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        // Always succeed from the client's perspective: the important part is
        // that the client clears its own local session regardless of whether
        // the server-side token happened to already be revoked/expired.
        return Ok(new { Message = "Logged out" });
    }
}
