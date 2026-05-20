namespace Omni2FA.Core.Configuration;

/// <summary>TOTP algorithm settings. Defaults match RFC 6238 with 6-digit codes and 30-second window.</summary>
public class TotpOptions {
    /// <summary>
    /// Issuer name shown in the authenticator app next to the account ("MyApp" in the entry
    /// "MyApp: alice@example.com"). Used in the otpauth:// URI.
    /// </summary>
    public string Issuer { get; set; } = "Omni2FA";

    /// <summary>Length of the random TOTP secret in bytes. 20 = RFC 6238 recommendation.</summary>
    public int SecretByteLength { get; set; } = 20;

    /// <summary>Number of digits in the generated code. Standard is 6.</summary>
    public int Digits { get; set; } = 6;

    /// <summary>Length of one TOTP step in seconds. Standard is 30.</summary>
    public int PeriodSeconds { get; set; } = 30;

    /// <summary>
    /// How many ±steps of clock skew the verifier accepts. 1 means ±30 seconds either side
    /// of the current step. Higher values widen the window but increase brute-force surface.
    /// </summary>
    public int ToleranceSteps { get; set; } = 1;
}
