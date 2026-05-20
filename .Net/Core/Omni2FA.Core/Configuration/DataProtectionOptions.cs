namespace Omni2FA.Core.Configuration;

/// <summary>
/// Data Protection settings. Hosts migrating from a custom 2FA implementation must set
/// <see cref="Scope"/> to the same value used in the old code so existing TOTP secrets
/// decrypt without users having to re-enroll.
/// </summary>
public class DataProtectionOptions {
    /// <summary>
    /// Purpose string passed to <c>IDataProtectionProvider.CreateProtector</c>.
    /// Defaults to <c>"Omni2FA"</c> for greenfield projects. Override for migrations.
    /// </summary>
    public string Scope { get; set; } = "Omni2FA";
}
