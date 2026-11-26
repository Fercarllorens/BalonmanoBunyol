using System.Security.Claims;
using BalonmanoBunol.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService auth, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Headers.CacheControl = "no-store, no-cache";
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RegisterAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.User) : BadRequest(new { message = result.Error });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        return result.Succeeded ? Ok(result.User) : Unauthorized(new { message = result.Error });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> CurrentUser(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var user = await auth.GetCurrentUserAsync(userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(cancellationToken);
        return NoContent();
    }
}
