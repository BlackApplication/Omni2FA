using Example.Backend.Dtos.Auth;
using Example.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Example.Backend.Controllers;

[Route("auth")]
public class AuthController : ApiBaseController {
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken) {
        var result = await _auth.RegisterAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) {
        var result = await _auth.LoginAsync(request, cancellationToken);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpPost("finalize")]
    public async Task<IActionResult> Finalize([FromBody] FinalizeRequest request, CancellationToken cancellationToken) {
        var result = await _auth.FinalizeAfter2FaAsync(request.UserId, cancellationToken);
        return HandleResult(result);
    }
}
