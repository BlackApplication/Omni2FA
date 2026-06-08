using System.Security.Cryptography;
using System.Text;

namespace Omni2FA.Core.Helpers;

/// <summary>Generation, normalization, and hashing of recovery codes. Format: <c>XXXX-XXXX-XX</c>, 40 bits of entropy.</summary>
public static class RecoveryCodeHelpers {
    /// <summary>Generate one code as <c>XXXX-XXXX-XX</c> (5 random bytes → 10 hex chars, grouped).</summary>
    public static string GenerateCode() {
        var bytes = RandomNumberGenerator.GetBytes(5);
        var hex = Convert.ToHexString(bytes);
        return $"{hex[..4]}-{hex.Substring(4, 4)}-{hex.Substring(8, 2)}";
    }

    /// <summary>Strip dashes/spaces and upper-case, so user formatting differences don't matter.</summary>
    public static string Normalize(string code) {
        return code
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
    }

    /// <summary>SHA-256 hash (lower-case hex) of the normalized code. High entropy → no per-code salt needed.</summary>
    public static string Hash(string code) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(code)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
