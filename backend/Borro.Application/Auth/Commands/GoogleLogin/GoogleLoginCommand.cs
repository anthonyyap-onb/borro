using Borro.Application.Auth.DTOs;
using MediatR;

namespace Borro.Application.Auth.Commands;

public record GoogleLoginCommand(string AccessToken) : IRequest<AuthResult>;
