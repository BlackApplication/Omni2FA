using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.Core.Services;

/// <summary>
/// JWT-based <see cref="IPreAuthTokenIssuer"/>. HMAC-SHA256 signed, stateless — validation
/// does not touch the database.
/// </summary>
public class JwtPreAuthTokenIssuer : IPreAuthTokenIssuer {
    /// <summary>Claim used to disambiguate Omni2FA pre-auth tokens from regular session tokens.</summary>
    public const string PurposeClaim = "omni2fa_purpose";

    /// <summary>Value of <see cref="PurposeClaim"/> for pre-auth (challenge-pending) tokens.</summary>
    public const string PurposeValue = "2fa-pending";

    /// <summary>Value of <see cref="PurposeClaim"/> for verified-handoff tokens (challenge passed).</summary>
    public const string PurposeVerifiedValue = "2fa-verified";

    /// <summary>Value of <see cref="PurposeClaim"/> for step-up (action-confirmation) tokens.</summary>
    public const string PurposeStepUpValue = "2fa-stepup";

    private readonly PreAuthOptions _options;
    private readonly TimeSpan _stepUpTtl;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;
    private readonly JwtSecurityTokenHandler _handler = new() {
        // Read claims as-is — we look up 'sub' directly. Without this the static
        // DefaultInboundClaimTypeMap rewrites 'sub' to ClaimTypes.NameIdentifier
        // and the lookup misses it.
        MapInboundClaims = false,
    };

    public JwtPreAuthTokenIssuer(IOptions<Omni2FaOptions> options) {
        _options = options.Value.PreAuth;
        _stepUpTtl = options.Value.StepUp.Ttl;
        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        _validationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero,
        };
    }

    public PreAuthTokenInfo Issue(string userId) => CreateSignedToken(userId, PurposeValue, _options.Ttl);

    public PreAuthTokenInfo IssueVerified(string userId) => CreateSignedToken(userId, PurposeVerifiedValue, _options.VerifiedTtl);

    public PreAuthTokenInfo IssueStepUp(string userId) => CreateSignedToken(userId, PurposeStepUpValue, _stepUpTtl);

    public string? ValidateAndGetUserId(string token) => Validate(token, PurposeValue);

    public string? ValidateVerified(string token) => Validate(token, PurposeVerifiedValue);

    public StepUpTokenClaims? ValidateStepUp(string token) {
        if (string.IsNullOrWhiteSpace(token)) {
            return null;
        }
        try {
            var principal = _handler.ValidateToken(token, _validationParameters, out var validated);
            if (principal.FindFirst(PurposeClaim)?.Value != PurposeStepUpValue) {
                return null;
            }
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(jti)) {
                return null;
            }
            return new StepUpTokenClaims(sub, jti, validated.ValidTo);
        } catch {
            return null;
        }
    }

    /// <summary>Build, sign, and encode a JWT for the user carrying the given purpose claim and lifetime.</summary>
    private PreAuthTokenInfo CreateSignedToken(string userId, string purpose, TimeSpan ttl) {
        if (string.IsNullOrWhiteSpace(userId)) {
            throw new ArgumentException("userId must not be empty.", nameof(userId));
        }
        var now = DateTime.UtcNow;
        var expires = now.Add(ttl);
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(PurposeClaim, purpose),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _signingCredentials);
        var encoded = _handler.WriteToken(token);
        return new PreAuthTokenInfo(encoded, expires);
    }

    private string? Validate(string token, string requiredPurpose) {
        if (string.IsNullOrWhiteSpace(token)) {
            return null;
        }
        try {
            var principal = _handler.ValidateToken(token, _validationParameters, out _);
            // Both kinds share signature/issuer/audience — the purpose claim keeps them apart.
            if (principal.FindFirst(PurposeClaim)?.Value != requiredPurpose) {
                return null;
            }
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return string.IsNullOrWhiteSpace(sub) ? null : sub;
        } catch {
            return null;
        }
    }
}
