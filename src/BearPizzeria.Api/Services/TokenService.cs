using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BearPizzeria.Api.Services;

public interface ITokenService
{
    string GenerarToken(int clienteId, string username, string email);
    (bool EsValido, int? ClienteId, string? Username) ValidarToken(string? token);
}

public class TokenService : ITokenService
{
    private readonly byte[] _secretKeyBytes;

    public TokenService(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("Configuración 'Jwt:Key' es requerida y debe contener al menos 32 caracteres.");
        }
        _secretKeyBytes = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerarToken(int clienteId, string username, string email)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var headerJson = JsonSerializer.Serialize(header);
        var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));

        var payload = new
        {
            sub = clienteId.ToString(),
            clienteId = clienteId,
            username = username,
            email = email,
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()
        };
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        var unsignedToken = $"{headerBase64}.{payloadBase64}";
        using var hmac = new HMACSHA256(_secretKeyBytes);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
        var signatureBase64 = Base64UrlEncode(signatureBytes);

        return $"{unsignedToken}.{signatureBase64}";
    }

    public (bool EsValido, int? ClienteId, string? Username) ValidarToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, null, null);

        // Soporte si viene con prefijo "Bearer "
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token[7..].Trim();

        var parts = token.Split('.');
        if (parts.Length != 3)
            return (false, null, null);

        var headerBase64 = parts[0];
        var payloadBase64 = parts[1];
        var signatureBase64 = parts[2];

        var unsignedToken = $"{headerBase64}.{payloadBase64}";
        using var hmac = new HMACSHA256(_secretKeyBytes);
        var expectedSignatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
        var expectedSignatureBase64 = Base64UrlEncode(expectedSignatureBytes);

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signatureBase64),
            Encoding.UTF8.GetBytes(expectedSignatureBase64)))
        {
            return (false, null, null);
        }

        try
        {
            var payloadBytes = Base64UrlDecode(payloadBase64);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("exp", out var expElem))
            {
                var exp = expElem.GetInt64();
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                    return (false, null, null); // Token expirado
            }

            int clienteId = 0;
            if (root.TryGetProperty("clienteId", out var cElem))
                clienteId = cElem.GetInt32();
            else if (root.TryGetProperty("sub", out var subElem) && int.TryParse(subElem.GetString(), out var subId))
                clienteId = subId;

            string? username = null;
            if (root.TryGetProperty("username", out var uElem))
                username = uElem.GetString();

            if (clienteId > 0)
                return (true, clienteId, username);

            return (false, null, null);
        }
        catch
        {
            return (false, null, null);
        }
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }
}
