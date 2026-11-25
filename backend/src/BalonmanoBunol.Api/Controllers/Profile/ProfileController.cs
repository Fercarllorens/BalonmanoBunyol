using System.Security.Claims;
using BalonmanoBunol.Application.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace BalonmanoBunol.Api.Controllers.Profile;

[ApiController]
[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController(IProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await profiles.GetAsync(userId, cancellationToken);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await profiles.UpdateAsync(userId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Profile) : BadRequest(new { message = result.Error });
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SetAvatar(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (Request.ContentLength is > 5_300_000) return BadRequest(new { message = "La imagen debe pesar 5 MB o menos." });
        var requestSizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (requestSizeFeature is { IsReadOnly: false }) requestSizeFeature.MaxRequestBodySize = 5_300_000;
        IFormCollection form;
        try { form = await Request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException) { return BadRequest(new { message = "El archivo supera el tamaño máximo permitido." }); }
        var file = form.Files.GetFile("avatar");
        if (file is null) return BadRequest(new { message = "Selecciona una imagen de perfil." });
        if (file.Length > 5 * 1024 * 1024) return BadRequest(new { message = "La imagen debe pesar 5 MB o menos." });
        await using var stream = file.OpenReadStream();
        var result = await profiles.SetAvatarAsync(userId, file.ContentType, file.Length, stream, cancellationToken);
        return result.Succeeded ? Ok(result.Profile) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> RemoveAvatar(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await profiles.RemoveAvatarAsync(userId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("avatar/{userId:guid}")]
    public async Task<IActionResult> GetAvatar(Guid userId, CancellationToken cancellationToken)
    {
        var avatar = await profiles.OpenAvatarAsync(userId, cancellationToken);
        return avatar is null ? NotFound() : File(avatar.Value.Content, avatar.Value.ContentType, enableRangeProcessing: false);
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
