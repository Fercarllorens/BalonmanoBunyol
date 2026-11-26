using System.Security.Claims;
using BalonmanoBunol.Application.Social;
using BalonmanoBunol.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.Social;

[ApiController]
[Route("api/v1/feed")]
public sealed class FeedController(IFeedService feed) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] int? pageSize, [FromQuery] DateTimeOffset? before, [FromQuery] Guid? beforeId, CancellationToken cancellationToken) =>
        Ok(await feed.GetLatestAsync(pageSize ?? 20, before, beforeId, officialOnly: false, cancellationToken));

    [HttpGet("announcements")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAnnouncements([FromQuery] int? pageSize, [FromQuery] DateTimeOffset? before, [FromQuery] Guid? beforeId, CancellationToken cancellationToken) =>
        Ok(await feed.GetLatestAsync(pageSize ?? 20, before, beforeId, officialOnly: true, cancellationToken));

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(201L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 201L * 1024 * 1024, ValueLengthLimit = 16 * 1024)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        const long maxRequestBytes = 201L * 1024 * 1024;
        if (Request.ContentLength is > maxRequestBytes)
            return BadRequest(new { message = "El tamaño total de los adjuntos supera el límite permitido." });
        if (Request.ContentLength is null && Request.Body.CanSeek && Request.Body.Length > maxRequestBytes)
            return BadRequest(new { message = "El tamaño total de los adjuntos supera el límite permitido." });
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var authorId)) return Unauthorized();

        IFormCollection form;
        try { form = await Request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException) { return BadRequest(new { message = "El tamaño o formato de la solicitud no es válido." }); }
        if (form.Files.Count > 4) return BadRequest(new { message = "Se permiten como máximo cuatro adjuntos por publicación." });
        if (form.Files.Any(file => file.Length > 50L * 1024 * 1024)) return BadRequest(new { message = "Cada vídeo debe pesar 50 MB o menos." });

        var streams = form.Files.Select(file => file.OpenReadStream()).ToArray();
        try
        {
            var attachments = form.Files.Select((file, index) => new CreatePostAttachment(file.FileName, file.ContentType, file.Length, streams[index])).ToArray();
            var authorName = User.FindFirstValue(ClaimTypes.Name) ?? "Usuario del club";
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ClubUser>>();
            var authenticatedUser = await userManager.FindByIdAsync(authorId.ToString());
            if (authenticatedUser is null || !authenticatedUser.IsActive) return Unauthorized();
            var authorRoles = (await userManager.GetRolesAsync(authenticatedUser)).ToArray();
            var authorAvatarUrl = authenticatedUser.AvatarStorageKey is null ? null : $"/api/v1/profile/avatar/{authorId}";
            var post = await feed.CreateAsync(new CreatePostCommand(authorId, authorName, authorRoles, form["content"].ToString(), attachments, authorAvatarUrl), cancellationToken);
            return Created($"/api/v1/feed/{post.Id}", post);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        finally
        {
            foreach (var stream in streams) await stream.DisposeAsync();
        }
    }

    [HttpDelete("{postId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid postId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var authorId)) return Unauthorized();
        return await feed.DeleteAsync(postId, authorId, cancellationToken)
            ? NoContent()
            : NotFound(new { message = "La publicación no existe o no puedes eliminarla." });
    }

    [HttpGet("media/{attachmentId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMedia(Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await feed.OpenPublicAttachmentAsync(attachmentId, cancellationToken);
        return attachment is null
            ? NotFound()
            : File(attachment.Value.Content, attachment.Value.ContentType, attachment.Value.FileName,
                enableRangeProcessing: attachment.Value.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));
    }
}
