using System.Security.Cryptography;
using System.Text;
using Example.Backend.Services.Interfaces;

namespace Example.Backend.Services;

/// <summary>
/// Demo-grade password hasher — PBKDF2 with SHA-256, 100k iterations, 16-byte salt. Stored format:
/// <c>{iterations}.{base64(salt)}.{base64(hash)}</c>. Not for production — use BCrypt/Argon2id instead.
/// </summary>
public class PasswordHasher : IPasswordHasher {
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string plaintext) {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(plaintext), salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string plaintext, string stored) {
        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) {
            return false;
        }
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(plaintext), salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
