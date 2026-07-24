using CarPlates.Application.Common.DTOs;
using MediatR;

namespace CarPlates.Application.Authentication.Commands;

public record LoginCommand(string Username, string Password, DeviceInfoDto? Device = null) : IRequest<LoginResult>;

public record LoginResult(bool Success, string? AccessToken, string? RefreshToken, UserDto? User, string? ErrorMessage, bool DeviceBlocked = false);
