using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services.Interfaces;
using OtpNet;

namespace Omni2FA.Core.Services;

/// <summary>Default TOTP service implementation built on <c>OtpNet</c>.</summary>
public class TotpService : ITotpService {
    private readonly TotpOptions _options;

    /// <summary>Construct with options bound from configuration.</summary>
    public TotpService(IOptions<Omni2FaOptions> options) {
        _options = options.Value.Totp;
    }

    /// <inheritdoc />
    public string GenerateSecret() {
        var bytes = RandomNumberGenerator.GetBytes(_options.SecretByteLength);
        return Base32Encoding.ToString(bytes);
    }

    /// <inheritdoc />
    public string BuildOtpAuthUri(string accountLabel, string base32Secret) {
        var label = Uri.EscapeDataString($"{_options.Issuer}:{accountLabel}");
        var issuer = Uri.EscapeDataString(_options.Issuer);
        return $"otpauth://totp/{label}?secret={base32Secret}&issuer={issuer}&algorithm=SHA1&digits={_options.Digits}&period={_options.PeriodSeconds}";
    }

    /// <inheritdoc />
    public bool ValidateCode(string base32Secret, string code) {
        if (string.IsNullOrWhiteSpace(code)) {
            return false;
        }
        var normalized = code.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (normalized.Length != _options.Digits || !normalized.All(char.IsDigit)) {
            return false;
        }
        var totp = new Totp(Base32Encoding.ToBytes(base32Secret), step: _options.PeriodSeconds, totpSize: _options.Digits);
        var window = new VerificationWindow(_options.ToleranceSteps, _options.ToleranceSteps);
        return totp.VerifyTotp(normalized, out _, window);
    }
}
