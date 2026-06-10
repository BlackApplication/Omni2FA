using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

internal static class RecoveryCodesEndpoints {
    public static void Map(IEndpointRouteBuilder root, bool requireStepUp) {
        var group = root.MapGroup("/recovery-codes");

        var regenerate = group.MapPost("/regenerate", async (
            IRecoveryCodeService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RegenerateAsync(user.GetCurrentUserId(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("regenerateRecoveryCodes")
        .WithTags("recovery-codes")
        .Produces<RecoveryCodesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        if (requireStepUp) {
            regenerate.RequireStepUp();
        }
    }
}
