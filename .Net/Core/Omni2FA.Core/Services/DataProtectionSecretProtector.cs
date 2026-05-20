using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.Core.Services;

/// <summary>
/// Default <see cref="ISecretProtector"/> built on ASP.NET Data Protection. Scope is taken
/// from <c>DataProtectionOptions.Scope</c>, so hosts migrating from a custom 2FA can point
/// at their existing scope and reuse stored secrets.
/// </summary>
public class DataProtectionSecretProtector : ISecretProtector {
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider, IOptions<Omni2FaOptions> options) {
        _protector = provider.CreateProtector(options.Value.DataProtection.Scope);
    }

    public string Protect(string plaintext) {
        return _protector.Protect(plaintext);
    }

    public string Unprotect(string protectedValue) {
        return _protector.Unprotect(protectedValue);
    }
}
