using Example.Backend.Dtos.Auth;
using Example.Backend.Entities;
using Example.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Results;
using Omni2FA.Core.Services.Interfaces;
using Omni2FA.Core.Stores;

namespace Example.Backend.Services;

public class AuthService : IAuthService {
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwords;
    private readonly IHostSessionIssuer _sessionIssuer;
    private readonly IPreAuthTokenIssuer _preAuthIssuer;
    private readonly ITwoFactorMethodStore _methods;

    public AuthService(
        AppDbContext db,
        IPasswordHasher passwords,
        IHostSessionIssuer sessionIssuer,
        IPreAuthTokenIssuer preAuthIssuer,
        ITwoFactorMethodStore methods) {
        _db = db;
        _passwords = passwords;
        _sessionIssuer = sessionIssuer;
        _preAuthIssuer = preAuthIssuer;
        _methods = methods;
    }

    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(emailNormalized) || string.IsNullOrWhiteSpace(request.Password)) {
            return Result<LoginResponse>.Failure("INVALID_INPUT", "Email and password are required.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == emailNormalized, cancellationToken).ConfigureAwait(false);
        if (exists) {
            return Result<LoginResponse>.Failure("EMAIL_TAKEN", "An account with that email already exists.");
        }

        var user = new User {
            Id = Guid.NewGuid(),
            Email = emailNormalized,
            PasswordHash = _passwords.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<LoginResponse>.Success(IssueSession(user));
    }

    public async Task<Result<AuthOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailNormalized, cancellationToken).ConfigureAwait(false);
        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash)) {
            return Result<AuthOutcome>.Failure("INVALID_CREDENTIALS", "Email or password is incorrect.");
        }

        var userIdString = user.Id.ToString();
        var has2Fa = await _methods.HasActiveMethodsAsync(userIdString, cancellationToken).ConfigureAwait(false);
        if (!has2Fa) {
            return Result<AuthOutcome>.Success(new AuthOutcome {
                Kind = AuthOutcomeKind.Session,
                Session = IssueSession(user),
            });
        }

        var preAuth = _preAuthIssuer.Issue(userIdString);
        var enrolled = await _methods.ListActiveByUserAsync(userIdString, cancellationToken).ConfigureAwait(false);
        var availableMethods = enrolled.Select(m => new TwoFactorMethodDto {
            Id = m.Id,
            Type = m.Type,
            Name = m.Name,
            CreatedAt = m.CreatedAt,
            LastUsedAt = m.LastUsedAt,
        }).ToList();

        return Result<AuthOutcome>.Success(new AuthOutcome {
            Kind = AuthOutcomeKind.Challenge,
            Challenge = new PreAuthChallengeResponse {
                PreAuthToken = preAuth.Token,
                AvailableMethods = availableMethods,
                ExpiresAt = preAuth.ExpiresAt,
            },
        });
    }

    public async Task<Result<LoginResponse>> FinalizeAfter2FaAsync(string? verifiedToken, CancellationToken cancellationToken = default) {
        // Derive the user from the verified-handoff token — proof 2FA actually passed, not just the password.
        if (string.IsNullOrWhiteSpace(verifiedToken)) {
            return Result<LoginResponse>.Failure("INVALID_PREAUTH", "Missing verified token.");
        }
        var userIdString = _preAuthIssuer.ValidateVerified(verifiedToken);
        if (userIdString is null || !Guid.TryParse(userIdString, out var userId)) {
            return Result<LoginResponse>.Failure("INVALID_PREAUTH", "Verified token is invalid or expired.");
        }
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result<LoginResponse>.Failure("USER_NOT_FOUND", "User no longer exists.");
        }
        return Result<LoginResponse>.Success(IssueSession(user));
    }

    private LoginResponse IssueSession(User user) {
        return new LoginResponse {
            SessionToken = _sessionIssuer.Issue(user.Id, user.Email),
            UserId = user.Id,
            Email = user.Email,
        };
    }
}
