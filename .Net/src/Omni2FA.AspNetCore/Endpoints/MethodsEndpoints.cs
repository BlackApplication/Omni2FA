using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

internal static class MethodsEndpoints {
    public static void Map(IEndpointRouteBuilder group) {
        group.MapGet("/methods", async (
            ITwoFactorMethodService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var methods = await service.ListAsync(user.GetCurrentUserId(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(methods);
        })
        .WithName("listMethods")
        .WithTags("methods")
        .Produces<IReadOnlyList<TwoFactorMethodDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/methods/{methodId:guid}", async (
            Guid methodId,
            ITwoFactorMethodService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RemoveAsync(user.GetCurrentUserId(), methodId, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("removeMethod")
        .WithTags("methods")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
