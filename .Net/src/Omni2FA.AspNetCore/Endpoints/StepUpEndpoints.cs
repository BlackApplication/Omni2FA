using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Filters;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

/// <summary>
/// Step-up (action-confirmation) endpoints for an already-authenticated user. Mirror the login
/// <c>/challenge/*</c> flow, but the user is resolved from the host session (not a pre-auth token) and
/// verify mints a single-use step-up token instead of the login handoff token.
/// </summary>
internal static class StepUpEndpoints {
    public static void Map(IEndpointRouteBuilder root) {
        var group = root.MapGroup("/stepup")
            .RequireAuthorization()
            .AddEndpointFilter<RateLimitFilter>();

        group.MapPost("/start", async (
            ChallengeStartRequest request,
            ITwoFactorChallengeService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.StartAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("startStepUp")
        .WithTags("stepup")
        .Accepts<ChallengeStartRequest>("application/json")
        .Produces<ChallengeStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/resend", async (
            ChallengeResendRequest request,
            ITwoFactorChallengeService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ResendAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("resendStepUp")
        .WithTags("stepup")
        .Accepts<ChallengeResendRequest>("application/json")
        .Produces<ChallengeStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/verify", async (
            ChallengeVerifyRequest request,
            ITwoFactorChallengeService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.VerifyStepUpAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("verifyStepUp")
        .WithTags("stepup")
        .Accepts<ChallengeVerifyRequest>("application/json")
        .Produces<StepUpVerifyResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
