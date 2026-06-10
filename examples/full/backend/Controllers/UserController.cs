using System.Security.Claims;
using Example.Backend.Dtos.Auth;
using Example.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omni2FA.AspNetCore.Filters;

namespace Example.Backend.Controllers;

[Route("user")]
[Authorize]
public class UserController : ApiBaseController {
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwords;

    public UserController(AppDbContext db, IPasswordHasher passwords) {
        _db = db;
        _passwords = passwords;
    }

    [HttpGet("me")]
    public IActionResult Me() {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (userId is null || email is null) {
            return Unauthorized();
        }
        return Ok(new { userId, email });
    }

    /// <summary>
    /// Sensitive action protected by step-up 2FA. <see cref="RequireTwoFactorAttribute"/> forces the user
    /// to confirm a fresh 2FA before the password changes — if they have a method enrolled, the request
    /// returns <c>403 STEP_UP_REQUIRED</c> until a valid step-up token is presented; if they have no 2FA,
    /// it proceeds. A stolen session token alone can't change the password.
    /// </summary>
    [HttpPost("change-password")]
    [RequireTwoFactor]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken) {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null || !Guid.TryParse(userId, out var id)) {
            return Unauthorized();
        }
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) {
            return Unauthorized();
        }
        if (!_passwords.Verify(request.CurrentPassword, user.PasswordHash)) {
            return Problem("Current password is incorrect.", statusCode: StatusCodes.Status400BadRequest, title: "INVALID_CREDENTIALS");
        }

        user.PasswordHash = _passwords.Hash(request.NewPassword);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
