using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BusinessOps.Backend.Models;

namespace BusinessOps.Backend.Auth;

public class JwtTokenService(IOptions<AuthOptions> options)
{
    private readonly AuthOptions _options = options.Value;

    public (string token, DateTime expiresAt) CreateToken(User user, string roleName)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["name"] = user.FullName,
            ["role"] = roleName,
            ["roleId"] = user.RoleId.ToString(),
            ["roleName"] = roleName,
            ["iss"] = _options.Issuer,
            ["aud"] = _options.Audience,
            ["exp"] = new DateTimeOffset(expiresAt).ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";
        var signature = ComputeSignature(unsignedToken, _options.SecretKey);

        return ($"{unsignedToken}.{signature}", expiresAt);
    }

    public static bool TryValidateToken(string token, AuthOptions options, out Dictionary<string, JsonElement> payload)
    {
        payload = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        var expectedSignature = ComputeSignature(unsignedToken, options.SecretKey);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(parts[2]),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return false;
        }

        try
        {
            var payloadJson = JsonDocument.Parse(Base64UrlDecode(parts[1]));
            payload = payloadJson.RootElement
                .EnumerateObject()
                .ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            if (!payload.TryGetValue("iss", out var issuer) || issuer.GetString() != options.Issuer)
            {
                return false;
            }

            if (!payload.TryGetValue("aud", out var audience) || audience.GetString() != options.Audience)
            {
                return false;
            }

            if (!payload.TryGetValue("exp", out var expElement) ||
                !expElement.TryGetInt64(out var expSeconds))
            {
                return false;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            if (expiresAt < DateTimeOffset.UtcNow)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeSignature(string unsignedToken, string secretKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
        return Base64UrlEncode(signatureBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        if (base64.Length % 4 != 0)
        {
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4), '=');
        }

        return Convert.FromBase64String(base64);
    }
}

public class AuthOptions
{
    public string SecretKey { get; set; } = "development-secret-key-change-me-please-2026";

    public string Issuer { get; set; } = "BusinessOps";

    public string Audience { get; set; } = "BusinessOpsClient";

    public int ExpiryMinutes { get; set; } = 480;
}
