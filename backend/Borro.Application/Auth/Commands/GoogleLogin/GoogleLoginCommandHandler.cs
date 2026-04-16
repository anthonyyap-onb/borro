using Borro.Application.Auth.DTOs;
using Borro.Application.Common.Interfaces;
using Borro.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Borro.Application.Auth.Commands;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly IJwtService _jwtService;
    private readonly TimeProvider _timeProvider;

    public GoogleLoginCommandHandler(
        IApplicationDbContext context,
        IGoogleTokenVerifier googleTokenVerifier,
        IJwtService jwtService,
        TimeProvider timeProvider)
    {
        _context = context;
        _googleTokenVerifier = googleTokenVerifier;
        _jwtService = jwtService;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var payload = await _googleTokenVerifier.VerifyAsync(request.AccessToken, cancellationToken)
            ?? throw new InvalidOperationException("Invalid Google access token.");

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                // Google-authenticated users have no local password hash
                PasswordHash = string.Empty,
                FirstName = payload.FirstName ?? string.Empty,
                LastName = payload.LastName ?? string.Empty,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AuthResult(_jwtService.GenerateToken(user), user.Id, user.Email, user.FirstName, user.LastName);
    }
}
