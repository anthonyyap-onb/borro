using Borro.Application.Auth.DTOs;
using MediatR;

namespace Borro.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>;
