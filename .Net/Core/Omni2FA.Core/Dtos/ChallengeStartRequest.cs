namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/challenge/start</c>.</summary>
public class ChallengeStartRequest {
    /// <summary>Id of the method the user picked to verify with.</summary>
    public required Guid MethodId { get; init; }
}
