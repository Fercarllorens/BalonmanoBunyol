using System.Security.Claims;
using BalonmanoBunol.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.Administration;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/v1/admin/role-requests")]
public sealed class RoleRequestsController(IAuthService auth) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool? pendingOnly, CancellationToken cancellationToken)
    {
        var requests = await auth.GetRoleRequestsAsync(cancellationToken);
        var selected = pendingOnly == true ? requests.Where(request => request.Status == "Pending") : requests;
        return Ok(selected.Select(request => new
        {
            id = request.Id,
            userId = request.UserId,
            displayName = request.DisplayName,
            email = request.Email,
            requestedRole = request.RequestedRole,
            status = request.Status,
            createdAt = request.CreatedAt,
            resolvedAt = request.ResolvedAt,
            resolvedByUserId = request.ResolvedByUserId
        }));
    }

    [HttpPost("{requestId:guid}/approve")]
    public Task<IActionResult> Approve(Guid requestId, CancellationToken cancellationToken) => Resolve(requestId, approve: true, cancellationToken);

    [HttpPost("{requestId:guid}/reject")]
    public Task<IActionResult> Reject(Guid requestId, CancellationToken cancellationToken) => Resolve(requestId, approve: false, cancellationToken);

    private async Task<IActionResult> Resolve(Guid requestId, bool approve, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var administratorId)) return Unauthorized();
        return await auth.ResolveRoleRequestAsync(requestId, administratorId, approve, cancellationToken)
            ? NoContent()
            : Conflict(new { message = "La solicitud no existe o ya ha sido resuelta." });
    }
}
