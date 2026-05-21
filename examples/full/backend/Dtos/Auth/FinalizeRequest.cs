namespace Example.Backend.Dtos.Auth;

/// <summary>After <c>/api/2fa/challenge/verify</c> succeeds, frontend calls this with the verified <c>userId</c> to receive the final session token.</summary>
public class FinalizeRequest {
    public required Guid UserId { get; init; }
}
