using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Example.Backend.Configuration;
using Example.Backend.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Example.Backend.Services;

public class HostSessionIssuer : IHostSessionIssuer {
    private readonly HostJwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _handler = new();

    public HostSessionIssuer(IOptions<HostJwtOptions> options) {
        _options = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string Issue(Guid userId, string email) {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            notBefore: now,
            expires: now.Add(_options.Ttl),
            signingCredentials: _signingCredentials);
        return _handler.WriteToken(token);
    }
}
