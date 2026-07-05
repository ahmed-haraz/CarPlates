using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using MediatR;

namespace CarPlates.Application.Authentication.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAuthenticationService _authService;

    public LoginCommandHandler(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);

        if (!result.Success)
        {
            return new LoginResult(false, null, null, null, result.ErrorMessage);
        }

        var user = await _authService.GetCurrentUserAsync(cancellationToken);
        var userDto = user != null 
            ? new UserDto(user.Username, user.Email, user.FullName, user.ProfilePhotoUrl, user.Role)
            : null;

        return new LoginResult(true, result.AccessToken, result.RefreshToken, userDto, null);
    }
}
