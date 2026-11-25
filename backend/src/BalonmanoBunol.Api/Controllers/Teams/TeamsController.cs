using System.Security.Claims;
using BalonmanoBunol.Application.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.Teams;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class TeamsController(ITeamSpaceService teams) : ControllerBase
{
    [HttpGet("my-teams")]
    public async Task<IActionResult> MyTeams(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await teams.GetMyTeamsAsync(userId, cancellationToken));
    }

    [HttpGet("teams/{teamId:guid}/space")]
    public async Task<IActionResult> Space(Guid teamId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var space = await teams.GetTeamAsync(userId, teamId, cancellationToken);
        return space is null ? NotFound() : Ok(space);
    }

    [HttpGet("teams/{teamId:guid}/calendar")]
    public async Task<IActionResult> Calendar(Guid teamId, [FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var now = DateTime.UtcNow;
        try
        {
            var calendar = await teams.GetCalendarAsync(userId, teamId, year ?? now.Year, month ?? now.Month, cancellationToken);
            return calendar is null ? NotFound() : Ok(calendar);
        }
        catch (ArgumentOutOfRangeException) { return BadRequest(new { message = "El mes indicado no es válido." }); }
    }

    [HttpGet("teams/{teamId:guid}/players/search")]
    public async Task<IActionResult> SearchPlayers(Guid teamId, [FromQuery] string? query, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await teams.SearchPlayersAsync(userId, teamId, query ?? string.Empty, cancellationToken));
    }

    [HttpPost("teams/{teamId:guid}/players")]
    public async Task<IActionResult> AddPlayer(Guid teamId, TeamPlayerInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.AddPlayerAsync(userId, teamId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("teams/{teamId:guid}/players/{playerId:guid}")]
    public async Task<IActionResult> UpdatePlayerStats(Guid teamId, Guid playerId, TeamPlayerStatsInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.UpdatePlayerStatsAsync(userId, teamId, input with { UserId = playerId }, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("teams/{teamId:guid}/players/{playerId:guid}")]
    public async Task<IActionResult> RemovePlayer(Guid teamId, Guid playerId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.RemovePlayerAsync(userId, teamId, playerId, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("teams/{teamId:guid}/matches")]
    public async Task<IActionResult> CreateMatch(Guid teamId, TeamMatchInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.CreateMatchAsync(userId, teamId, input, cancellationToken);
        return result.Result.Succeeded
            ? Created($"/api/v1/teams/{teamId}/space", new { id = result.MatchId })
            : BadRequest(new { message = result.Result.Error });
    }

    [HttpPut("teams/{teamId:guid}/matches/{matchId:guid}/attendance")]
    public async Task<IActionResult> SetAttendance(Guid teamId, Guid matchId, TeamAttendanceInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.SetAttendanceAsync(userId, teamId, matchId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("teams/{teamId:guid}/events")]
    public async Task<IActionResult> CreateEvent(Guid teamId, TeamEventInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.CreateEventAsync(userId, teamId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("teams/{teamId:guid}/events/{eventId:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid teamId, Guid eventId, TeamEventInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.UpdateEventAsync(userId, teamId, eventId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("teams/{teamId:guid}/events/{eventId:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid teamId, Guid eventId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.DeleteEventAsync(userId, teamId, eventId, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("teams/{teamId:guid}/training-plan")]
    public async Task<IActionResult> SetTrainingPlan(Guid teamId, TeamTrainingPlanInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.SetTrainingPlanAsync(userId, teamId, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPut("teams/{teamId:guid}/training-plan/{slotId:guid}/exceptions/{date}")]
    public async Task<IActionResult> SetTrainingException(Guid teamId, Guid slotId, DateOnly date, TeamTrainingExceptionInput input, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.SetTrainingExceptionAsync(userId, teamId, slotId, date, input, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("teams/{teamId:guid}/training-plan/{slotId:guid}/exceptions/{date}")]
    public async Task<IActionResult> DeleteTrainingException(Guid teamId, Guid slotId, DateOnly date, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await teams.DeleteTrainingExceptionAsync(userId, teamId, slotId, date, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
