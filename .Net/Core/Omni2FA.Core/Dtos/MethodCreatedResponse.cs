namespace Omni2FA.Core.Dtos;

/// <summary>Returned by every successful <c>/enroll/*/confirm</c> endpoint.</summary>
public class MethodCreatedResponse {
    /// <summary>Identifier of the newly enrolled method.</summary>
    public required Guid MethodId { get; init; }
}
