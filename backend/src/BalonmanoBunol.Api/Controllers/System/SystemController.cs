using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.System;

[ApiController]
[AllowAnonymous]
public sealed class SystemController : ControllerBase
{
    [HttpGet("/api/health")]
    public IActionResult Health() => Ok(new
    {
        status = "healthy",
        service = "balonmano-bunol-api",
        timestamp = DateTimeOffset.UtcNow
    });

    [HttpGet("/api")]
    public IActionResult ApiInfo() => Ok(new
    {
        name = "Club Balonmano Buñol API",
        version = "v1",
        documentation = "/docs"
    });
}
