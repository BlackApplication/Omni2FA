namespace Omni2FA.Core.Services.Interfaces;

/// <summary>
/// Round-trip protection for sensitive strings (notably TOTP base32 secrets) before they
/// land in storage. Default implementation uses ASP.NET Data Protection; hosts may provide
/// their own (Azure Key Vault, HashiCorp Vault, custom KMS) by implementing this interface.
/// </summary>
public interface ISecretProtector {
    /// <summary>Encrypt a plaintext value for storage.</summary>
    string Protect(string plaintext);

    /// <summary>Decrypt a previously-protected value back to plaintext.</summary>
    string Unprotect(string protectedValue);
}
