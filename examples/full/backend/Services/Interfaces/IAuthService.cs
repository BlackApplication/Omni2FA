using Example.Backend.Dtos.Auth;
using Omni2FA.Core.Results;

namespace Example.Backend.Services.Interfaces;

public interface IAuthService {
    Task<Result<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthOutcome>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> FinalizeAfter2FaAsync(Guid userId, CancellationToken cancellationToken = default);
}
