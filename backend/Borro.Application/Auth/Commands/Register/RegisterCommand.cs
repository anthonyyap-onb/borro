using Borro.Application.Auth.DTOs;
using MediatR;

namespace Borro.Application.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<AuthResult>;
