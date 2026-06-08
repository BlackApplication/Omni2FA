namespace Omni2FA.Core.Dtos;

/// <summary>Returned by every successful <c>/enroll/*/confirm</c> endpoint.</summary>
public class MethodCreatedResponse {
    /// <summary>Identifier of the newly enrolled method.</summary>
    public required Guid MethodId { get; init; }

    /// <summary>
    /// Plaintext recovery codes — present only when this enrollment generated them (the user's first
    /// method). Shown once; the frontend must display them. Null on subsequent enrollments.
    /// </summary>
    public IReadOnlyList<string>? RecoveryCodes { get; init; }
}
