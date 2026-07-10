using CarPlates.Application.Authentication.Commands;
using CarPlates.Application.Common.Interfaces;
using MediatR;

namespace CarPlates.Application.Authentication.Handlers;

public class LogoutCommandHandler(IAuthenticationService authService) : IRequestHandler<LogoutCommand, bool>
{
    private readonly IAuthenticationService _authService = authService;

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(cancellationToken);
        return true;
    }
}
