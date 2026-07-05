using MediatR;

namespace CarPlates.Application.Authentication.Commands;

public record LogoutCommand : IRequest<bool>;
