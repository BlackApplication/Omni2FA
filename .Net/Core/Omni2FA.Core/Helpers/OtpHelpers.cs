using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Omni2FA.Core.Helpers;

/// <summary>Stateless helpers for numeric one-time codes — generation, hashing, constant-time comparison.</summary>
public static class OtpHelpers {
    /// <summary>Generate a uniformly random numeric code of <paramref name="digits"/> length (leading zeros preserved).</summary>
    public static string GenerateNumericOtp(int digits) {
        if (digits is < 1 or > 9) {
            throw new ArgumentOutOfRangeException(nameof(digits), digits, "Digit count must be between 1 and 9.");
        }
        var max = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    /// <summary>SHA-256 hash of a code, lower-case hex. Codes are short-lived and high-uniqueness, so no per-code salt is used.</summary>
    public static string HashOtp(string code) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Constant-time check of a submitted code against a stored hash.</summary>
    public static bool Verify(string code, string expectedHash) {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(expectedHash)) {
            return false;
        }
        var actual = Encoding.UTF8.GetBytes(HashOtp(code));
        var expected = Encoding.UTF8.GetBytes(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
