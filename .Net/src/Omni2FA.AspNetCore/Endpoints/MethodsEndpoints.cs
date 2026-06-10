using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

internal static class MethodsEndpoints {
    public static void Map(IEndpointRouteBuilder group, bool requireStepUpOnRemove) {
        group.MapGet("/methods", async (
            ITwoFactorMethodService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var methods = await service.ListAsync(user.GetCurrentUserId(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(methods);
        })
        .RequireAuthorization()
        .WithName("listMethods")
        .WithTags("methods")
        .Produces<IReadOnlyList<TwoFactorMethodDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        var remove = group.MapDelete("/methods/{methodId:guid}", async (
            Guid methodId,
            ITwoFactorMethodService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RemoveAsync(user.GetCurrentUserId(), methodId, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("removeMethod")
        .WithTags("methods")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        if (requireStepUpOnRemove) {
            remove.RequireStepUp();
        }
    }
}
