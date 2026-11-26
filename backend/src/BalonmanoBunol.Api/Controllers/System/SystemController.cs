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

    [HttpGet("/docs")]
    [Produces("text/html")]
    public IActionResult Documentation() => Content("""
        <!doctype html>
        <html lang="es">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>API del Club Balonmano Buñol</title></head>
        <body>
          <main>
            <h1>API del Club Balonmano Buñol · v1</h1>
            <p>La API usa JSON salvo las cargas multipart indicadas. Las operaciones que modifican datos requieren el encabezado <code>X-CSRF-TOKEN</code>. El inicio de sesión y el registro no requieren una sesión previa, pero sí ese token; las demás mutaciones requieren además una sesión activa.</p>
            <h2>Sesión y perfil</h2>
            <ul><li><code>GET /api/v1/auth/csrf</code> (obtener el token CSRF antes de una mutación)</li><li><code>POST /api/v1/auth/login</code></li><li><code>POST /api/v1/auth/register</code></li><li><code>GET /api/v1/auth/me</code></li><li><code>GET|PUT /api/v1/profile</code></li><li><code>POST|DELETE /api/v1/profile/avatar</code></li></ul>
            <h2>Feed</h2>
            <ul><li><code>GET|POST /api/v1/feed</code> (POST multipart/form-data)</li><li><code>GET /api/v1/feed/announcements</code></li><li><code>DELETE /api/v1/feed/{postId}</code></li></ul>
            <h2>Equipos</h2>
            <ul><li><code>GET /api/v1/my-teams</code></li><li><code>GET /api/v1/teams/{teamId}/space</code></li><li><code>GET /api/v1/teams/{teamId}/calendar</code></li><li><code>POST /api/v1/teams/{teamId}/board</code> (multipart/form-data)</li></ul>
            <h2>Administración</h2>
            <p>Las rutas <code>/api/v1/admin</code> requieren el rol Administrador.</p>
            <ul><li><code>GET /api/v1/admin/users</code></li><li><code>GET /api/v1/admin/role-requests</code></li><li><code>GET /api/v1/admin/teams</code></li></ul>
          </main>
        </body>
        </html>
        """, "text/html; charset=utf-8");
}
