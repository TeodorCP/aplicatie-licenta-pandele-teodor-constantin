using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BusinessOps.Backend.Auth;

public class SimpleJwtAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var headerValue = authorizationHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = headerValue["Bearer ".Length..].Trim();
        if (!JwtTokenService.TryValidateToken(token, _authOptions, out var payload))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token."));
        }

        var claims = new List<Claim>();

        AddClaimIfPresent(claims, payload, "sub", ClaimTypes.NameIdentifier);
        AddClaimIfPresent(claims, payload, "email", ClaimTypes.Email);
        AddClaimIfPresent(claims, payload, "name", ClaimTypes.Name);
        AddClaimIfPresent(claims, payload, "role", ClaimTypes.Role);
        AddClaimIfPresent(claims, payload, "roleId", "roleId");
        AddClaimIfPresent(claims, payload, "roleName", "roleName");

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static void AddClaimIfPresent(
        ICollection<Claim> claims,
        IReadOnlyDictionary<string, System.Text.Json.JsonElement> payload,
        string sourceName,
        string claimType)
    {
        if (payload.TryGetValue(sourceName, out var value))
        {
            var stringValue = value.GetString();
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                claims.Add(new Claim(claimType, stringValue));
            }
        }
    }
}
