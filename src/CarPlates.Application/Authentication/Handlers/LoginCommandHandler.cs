using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using MediatR;

namespace CarPlates.Application.Authentication.Handlers;

public class LoginCommandHandler(IAuthenticationService authService) : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAuthenticationService _authService = authService;

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password, request.Device, cancellationToken);

        if (!result.Success)
        {
            return new LoginResult(false, null, null, null, result.ErrorMessage, result.DeviceBlocked);
        }

        var user = result.info;
        var userDto = user != null 
            ? new UserDto(user.Id, user.Username, user.Email, user.FullName, user.BranchId, user.CashboxID, user.CarId, user.StoreId, user.SalesRepID, user.Usertype)
            : null;

        return new LoginResult(true, result.AccessToken, result.RefreshToken, userDto, null);
    }
}
