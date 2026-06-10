namespace Example.Backend.Dtos.Auth;

/// <summary>Body for <c>POST /user/change-password</c> — a sensitive action gated by step-up 2FA.</summary>
public class ChangePasswordRequest {
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
