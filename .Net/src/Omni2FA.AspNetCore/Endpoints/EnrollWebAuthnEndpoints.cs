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

internal static class EnrollWebAuthnEndpoints {
    public static void Map(IEndpointRouteBuilder root, Omni2FaAudienceOptions audience, bool requireStepUpOnStart) {
        var group = root.MapGroup("/enroll/webauthn").AddEndpointFilter<RateLimitFilter>();

        var start = group.MapPost("/start", async (
            IWebAuthnEnrollmentService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.StartAsync(user.GetCurrentUserId(), user.GetCurrentUserLabel(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireOmni2FaSession(audience)
        .WithName(EndpointNaming.For(audience, "startWebAuthnEnrollment"))
        .WithTags("enroll-webauthn")
        .Produces<WebAuthnEnrollStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        if (requireStepUpOnStart) {
            start.RequireStepUp();
        }

        group.MapPost("/confirm", async (
            WebAuthnEnrollConfirmRequest request,
            IWebAuthnEnrollmentService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ConfirmAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireOmni2FaSession(audience)
        .WithName(EndpointNaming.For(audience, "confirmWebAuthnEnrollment"))
        .WithTags("enroll-webauthn")
        .Accepts<WebAuthnEnrollConfirmRequest>("application/json")
        .Produces<MethodCreatedResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
