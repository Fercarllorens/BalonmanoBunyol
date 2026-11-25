using System.Security.Claims;
using BalonmanoBunol.Application.Administration;
using BalonmanoBunol.Application.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.Administration;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/v1/admin")]
public sealed class AdministrationController(IAdministrationService administration) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? query, [FromQuery] bool? coachesOnly, [FromQuery] bool? playersOnly, CancellationToken cancellationToken) =>
        Ok(await administration.SearchUsersAsync(query ?? string.Empty, coachesOnly == true, playersOnly == true, cancellationToken));

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams(CancellationToken cancellationToken) => Ok(await administration.GetTeamsAsync(cancellationToken));

    [HttpGet("teams/{teamId:guid}/players")]
    public async Task<IActionResult> GetTeamPlayers(Guid teamId, CancellationToken cancellationToken) =>
        Ok(await administration.GetTeamPlayersAsync(teamId, cancellationToken));

    [HttpPut("teams/{teamId:guid}/players/{playerId:guid}")]
    public async Task<IActionResult> AssignTeamPlayer(Guid teamId, Guid playerId, TeamPlayerInput input, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.AssignPlayerAsync(administratorId, playerId,
            new AdminPlayerAssignmentInput(teamId, input.JerseyNumber, input.Position), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("teams/{teamId:guid}/players/{playerId:guid}")]
    public async Task<IActionResult> RemoveTeamPlayer(Guid teamId, Guid playerId, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.RemovePlayerAsync(administratorId, playerId, teamId, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam(TeamInput input, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.CreateTeamAsync(administratorId, input, cancellationToken);
        return result.Succeeded ? Created("/api/v1/admin/teams", null) : BadRequest(new { message = result.Error });
    }

    [HttpPut("teams/{teamId:guid}")]
    public async Task<IActionResult> UpdateTeam(Guid teamId, TeamInput input, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.UpdateTeamAsync(administratorId, teamId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("teams/{teamId:guid}/active")]
    public async Task<IActionResult> SetTeamActive(Guid teamId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.SetTeamActiveAsync(administratorId, teamId, isActive, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("users/{userId:guid}/teams")]
    public async Task<IActionResult> AssignTeams(Guid userId, AssignTeamsInput input, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.AssignTeamsAsync(administratorId, userId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("users/{userId:guid}/player-assignment")]
    public async Task<IActionResult> AssignPlayer(Guid userId, AdminPlayerAssignmentInput input, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.AssignPlayerAsync(administratorId, userId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("users/{userId:guid}/player-assignment/{teamId:guid}")]
    public async Task<IActionResult> RemovePlayer(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.RemovePlayerAsync(administratorId, userId, teamId, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("users/{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid userId, ChangeRoleInput input, CancellationToken cancellationToken)
    {
        if (!TryGetAdministratorId(out var administratorId)) return Unauthorized();
        var result = await administration.ChangeRoleAsync(administratorId, userId, input, cancellationToken);
        if (result.Succeeded) return NoContent();
        return result.IsConflict
            ? Conflict(new { message = result.Error })
            : BadRequest(new { message = result.Error });
    }

    private bool TryGetAdministratorId(out Guid administratorId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out administratorId);
}
