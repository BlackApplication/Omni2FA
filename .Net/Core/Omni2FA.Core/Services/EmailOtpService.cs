using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Entities;
using Omni2FA.Core.Helpers;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.Core.Services;

/// <summary>Default Email OTP primitive — generates, emails, and verifies numeric codes against a challenge.</summary>
public class EmailOtpService : IEmailOtpService {
    private readonly IEmailDispatcher _dispatcher;
    private readonly IEmailMessageBuilder _builder;
    private readonly EmailOptions _options;

    public EmailOtpService(IEmailDispatcher dispatcher, IEmailMessageBuilder builder, IOptions<Omni2FaOptions> options) {
        _dispatcher = dispatcher;
        _builder = builder;
        _options = options.Value.Email;
    }

    public async Task IssueAsync(TwoFactorChallenge challenge, string email, CancellationToken cancellationToken = default) {
        var code = OtpHelpers.GenerateNumericOtp(_options.OtpDigits);
        var now = DateTime.UtcNow;
        challenge.EmailOtpHash = OtpHelpers.HashOtp(code);
        challenge.CreatedAt = now;
        challenge.ExpiresAt = now.Add(_options.Ttl);

        var message = await _builder.BuildOtpMessageAsync(email, code, _options.Ttl, cancellationToken).ConfigureAwait(false);
        await _dispatcher.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public bool Verify(TwoFactorChallenge challenge, string code) {
        if (challenge.EmailOtpHash is null || challenge.ExpiresAt <= DateTime.UtcNow) {
            return false;
        }
        var normalized = code.Trim().Replace(" ", "", StringComparison.Ordinal);
        return OtpHelpers.Verify(normalized, challenge.EmailOtpHash);
    }

    public DateTime ResendAvailableAt(TwoFactorChallenge challenge) {
        return challenge.CreatedAt.Add(_options.ResendCooldown);
    }
}
