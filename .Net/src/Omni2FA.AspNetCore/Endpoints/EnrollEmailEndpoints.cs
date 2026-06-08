using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

internal static class EnrollEmailEndpoints {
    public static void Map(IEndpointRouteBuilder root) {
        var group = root.MapGroup("/enroll/email");

        group.MapPost("/start", async (
            EmailEnrollStartRequest request,
            IEmailEnrollmentService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.StartAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("startEmailEnrollment")
        .WithTags("enroll-email")
        .Accepts<EmailEnrollStartRequest>("application/json")
        .Produces<EmailEnrollStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/confirm", async (
            EmailEnrollConfirmRequest request,
            IEmailEnrollmentService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ConfirmAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("confirmEmailEnrollment")
        .WithTags("enroll-email")
        .Accepts<EmailEnrollConfirmRequest>("application/json")
        .Produces<MethodCreatedResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/resend", async (
            EmailEnrollResendRequest request,
            IEmailEnrollmentService service,
            IUserContextAccessor user,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ResendAsync(user.GetCurrentUserId(), request, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("resendEmailEnrollment")
        .WithTags("enroll-email")
        .Accepts<EmailEnrollResendRequest>("application/json")
        .Produces<EmailEnrollStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
