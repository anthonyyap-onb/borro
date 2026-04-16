using Borro.Application.Auth.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly TimeProvider _timeProvider;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        TimeProvider timeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailTaken)
            throw new InvalidOperationException("Email is already registered.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResult(_jwtService.GenerateToken(user), user.Id, user.Email, user.FirstName, user.LastName);
    }
}
