using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Filters;
using Omni2FA.AspNetCore.Internal;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

internal static class EnrollTotpEndpoints {
    public static void Map(IEndpointRouteBuilder root, Omni2FaAudienceOptions audience, bool requireStepUpOnStart) {
        var group = root.MapGroup("/enroll/totp").AddEndpointFilter<RateLimitFilter>();

        var start = group.MapPost("/start", async (
            ITotpEnrollmentService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.StartAsync(user.GetCurrentUserId(), user.GetCurrentUserLabel(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireOmni2FaSession(audience)
        .WithName(EndpointNaming.For(audience, "startTotpEnrollment"))
        .WithTags("enroll-totp")
        .Produces<TotpEnrollStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        if (requireStepUpOnStart) {
            start.RequireStepUp();
        }

        group.MapPost("/confirm", async (
            TotpEnrollConfirmRequest request,
            ITotpEnrollmentService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ConfirmAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireOmni2FaSession(audience)
        .WithName(EndpointNaming.For(audience, "confirmTotpEnrollment"))
        .WithTags("enroll-totp")
        .Accepts<TotpEnrollConfirmRequest>("application/json")
        .Produces<MethodCreatedResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
