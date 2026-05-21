using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Example.Backend.Controllers;

[Route("user")]
[Authorize]
public class UserController : ApiBaseController {
    [HttpGet("me")]
    public IActionResult Me() {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (userId is null || email is null) {
            return Unauthorized();
        }
        return Ok(new { userId, email });
    }
}
