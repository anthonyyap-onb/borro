namespace Borro.Application.Common.Interfaces;

public record GoogleUserPayload(string Email, string? FirstName, string? LastName);

public interface IGoogleTokenVerifier
{
    /// <summary>Verifies a Google OAuth2 access token by calling Google's userinfo endpoint.
    /// Returns null if the token is invalid.</summary>
    Task<GoogleUserPayload?> VerifyAsync(string accessToken, CancellationToken cancellationToken);
}
