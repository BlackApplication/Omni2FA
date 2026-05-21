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

    /// <summary>Value of <see cref="PurposeClaim"/> for pre-auth tokens.</summary>
    public const string PurposeValue = "2fa-pending";

    private readonly PreAuthOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtPreAuthTokenIssuer(IOptions<Omni2FaOptions> options) {
        _options = options.Value.PreAuth;
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

    public PreAuthTokenInfo Issue(string userId) {
        if (string.IsNullOrWhiteSpace(userId)) {
            throw new ArgumentException("userId must not be empty.", nameof(userId));
        }
        var now = DateTime.UtcNow;
        var expires = now.Add(_options.Ttl);
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(PurposeClaim, PurposeValue),
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

    public string? ValidateAndGetUserId(string token) {
        if (string.IsNullOrWhiteSpace(token)) {
            return null;
        }
        try {
            var principal = _handler.ValidateToken(token, _validationParameters, out _);
            var purpose = principal.FindFirst(PurposeClaim)?.Value;
            if (purpose != PurposeValue) {
                return null;
            }
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return string.IsNullOrWhiteSpace(sub) ? null : sub;
        } catch {
            return null;
        }
    }
}
