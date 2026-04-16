namespace Borro.Application.Auth.DTOs;

public record AuthResult(string Token, Guid UserId, string Email, string FirstName, string LastName);
