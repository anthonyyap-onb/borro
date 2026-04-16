using Borro.Application.Auth.DTOs;
using Borro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken)
            ?? throw new InvalidOperationException("Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid email or password.");

        return new AuthResult(_jwtService.GenerateToken(user), user.Id, user.Email, user.FirstName, user.LastName);
    }
}
