using System.Net.Http.Headers;
using System.Text.Json;
using Borro.Application.Common.Interfaces;

namespace Borro.Infrastructure.Services;

public class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private static readonly HttpClient _httpClient = new();
    private const string UserinfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";

    public async Task<GoogleUserPayload?> VerifyAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserinfoEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("email", out var emailProp))
                return null;

            var email = emailProp.GetString();
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var firstName = root.TryGetProperty("given_name", out var fn) ? fn.GetString() : null;
            var lastName = root.TryGetProperty("family_name", out var ln) ? ln.GetString() : null;

            return new GoogleUserPayload(email, firstName, lastName);
        }
        catch
        {
            return null;
        }
    }
}
