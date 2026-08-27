using System.Security.Cryptography;

namespace BearPizzeria.Api.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32;  // 256 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char SegmentDelimiter = ':';

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize
        );

        return string.Join(
            SegmentDelimiter,
            Convert.ToHexString(salt),
            Convert.ToHexString(key),
            Iterations,
            Algorithm
        );
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var segments = passwordHash.Split(SegmentDelimiter);
        if (segments.Length != 4)
            return false;

        var salt = Convert.FromHexString(segments[0]);
        var expectedKey = Convert.FromHexString(segments[1]);
        var iterations = int.Parse(segments[2]);
        var algorithm = new HashAlgorithmName(segments[3]);

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            algorithm,
            expectedKey.Length
        );

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
