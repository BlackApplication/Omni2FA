using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Omni2FA.AspNetCore.Extensions;
using Omni2FA.AspNetCore.Filters;
using Omni2FA.AspNetCore.Services.Interfaces;
using Omni2FA.Core.Configuration;
using Omni2FA.Core.Dtos;
using Omni2FA.Core.Services.Interfaces;

namespace Omni2FA.AspNetCore.Endpoints;

internal static class EnrollEmailEndpoints {
    public static void Map(IEndpointRouteBuilder root, bool requireStepUpOnStart) {
        var group = root.MapGroup("/enroll/email").AddEndpointFilter<RateLimitFilter>();

        var start = group.MapPost("/start", async (
            EmailEnrollStartRequest request,
            IEmailEnrollmentService service,
            IUserContextAccessor user,
            IOptions<Omni2FaOptions> options,
            CancellationToken cancellationToken) =>
        {
            var email = options.Value.AspNetCore.EmailEnrollmentAddressSource == EmailEnrollmentAddressSource.HostSupplied
                ? request.Email
                : user.GetCurrentUserEmail();
            var result = await service.StartAsync(user.GetCurrentUserId(), email, cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization()
        .WithName("startEmailEnrollment")
        .WithTags("enroll-email")
        .Accepts<EmailEnrollStartRequest>("application/json")
        .Produces<EmailEnrollStartResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        if (requireStepUpOnStart) {
            start.RequireStepUp();
        }

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
