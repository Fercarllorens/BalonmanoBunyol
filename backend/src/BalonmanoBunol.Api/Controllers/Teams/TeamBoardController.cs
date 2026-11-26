using System.Security.Claims;
using BalonmanoBunol.Application.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.Teams;

[ApiController]
[Authorize]
[Route("api/v1/teams/{teamId:guid}")]
public sealed class TeamBoardController(ITeamSpaceService teams) : ControllerBase
{
    [HttpPost("board")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(401L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 401L * 1024 * 1024, ValueLengthLimit = 16 * 1024)]
    public async Task<IActionResult> CreatePost(Guid teamId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        const long maxRequestBodySize = 401L * 1024 * 1024;
        if (Request.ContentLength is > maxRequestBodySize)
            return BadRequest(new { message = "Los adjuntos superan el límite total de 400 MB." });
        IFormCollection form;
        try { form = await Request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException) { return BadRequest(new { message = "El tamaño o formato de la solicitud no es válido." }); }
        if (form.Files.Count > 4) return BadRequest(new { message = "Se permiten hasta cuatro archivos por mensaje." });

        var files = new List<TeamBoardFile>();
        foreach (var file in form.Files)
        {
            if (file.Length > 100 * 1024 * 1024)
                return BadRequest(new { message = "Cada archivo debe pesar 100 MB o menos." });
            files.Add(new TeamBoardFile(file.FileName, file.ContentType, file.Length, file.OpenReadStream()));
        }

        try
        {
            var result = await teams.CreateBoardPostAsync(userId, teamId, new TeamBoardInput(form["content"].ToString()), files, cancellationToken);
            return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
        }
        finally
        {
            foreach (var file in files) await file.Content.DisposeAsync();
        }
    }

    [HttpGet("board-files/{attachmentId:guid}")]
    public async Task<IActionResult> GetFile(Guid teamId, Guid attachmentId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var file = await teams.OpenBoardAttachmentAsync(userId, teamId, attachmentId, cancellationToken);
        return file is null
            ? NotFound()
            : File(file.Value.Content, file.Value.ContentType,
                file.Value.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || file.Value.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : file.Value.FileName,
                enableRangeProcessing: file.Value.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
