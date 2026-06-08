namespace Omni2FA.Core.Dtos;

/// <summary>Body for <c>POST /api/2fa/challenge/resend</c> — re-send the login OTP for an Email method.</summary>
public class ChallengeResendRequest {
    /// <summary>The method whose active login OTP should be re-sent.</summary>
    public required Guid MethodId { get; init; }
}
