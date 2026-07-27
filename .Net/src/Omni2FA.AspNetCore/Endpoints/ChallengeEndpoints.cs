using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Filters;
using Omni2FA.AspNetCore.Internal;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

internal static class ChallengeEndpoints {
    public static void Map(IEndpointRouteBuilder root, Omni2FaAudienceOptions audience) {
        var group = root.MapGroup("/challenge")
            .AddEndpointFilter<PreAuthFilter>()
            .AddEndpointFilter<RateLimitFilter>();

        group.MapPost("/start", async (
            ChallengeStartRequest request,
            ITwoFactorChallengeService service,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var userId = (string)http.Items[PreAuthContextItems.UserId]!;
            var result = await service.StartAsync(userId, request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName(EndpointNaming.For(audience, "startChallenge"))
        .WithTags("challenge")
        .Accepts<ChallengeStartRequest>("application/json")
        .Produces<ChallengeStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/resend", async (
            ChallengeResendRequest request,
            ITwoFactorChallengeService service,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var userId = (string)http.Items[PreAuthContextItems.UserId]!;
            var result = await service.ResendAsync(userId, request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName(EndpointNaming.For(audience, "resendChallenge"))
        .WithTags("challenge")
        .Accepts<ChallengeResendRequest>("application/json")
        .Produces<ChallengeStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/recovery-code", async (
            RecoveryCodeVerifyRequest request,
            IRecoveryCodeService service,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var userId = (string)http.Items[PreAuthContextItems.UserId]!;
            var result = await service.VerifyAsync(userId, request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName(EndpointNaming.For(audience, "verifyRecoveryCode"))
        .WithTags("challenge")
        .Accepts<RecoveryCodeVerifyRequest>("application/json")
        .Produces<VerifySuccessResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/verify", async (
            ChallengeVerifyRequest request,
            ITwoFactorChallengeService service,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var userId = (string)http.Items[PreAuthContextItems.UserId]!;
            var result = await service.VerifyAsync(userId, request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName(EndpointNaming.For(audience, "verifyChallenge"))
        .WithTags("challenge")
        .Accepts<ChallengeVerifyRequest>("application/json")
        .Produces<VerifySuccessResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
