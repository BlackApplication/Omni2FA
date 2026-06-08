namespace Omni2FA.Core.Dtos;

/// <summary>Returned by <c>POST /api/2fa/recovery-codes/regenerate</c>. Plaintext, shown once.</summary>
public class RecoveryCodesResponse {
    /// <summary>Fresh plaintext codes. Previous codes are now invalid.</summary>
    public required IReadOnlyList<string> RecoveryCodes { get; init; }
}
