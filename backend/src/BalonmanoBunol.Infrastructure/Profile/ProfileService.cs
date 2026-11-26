using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BalonmanoBunol.Application.Profile;
using BalonmanoBunol.Domain.Teams;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BalonmanoBunol.Infrastructure.Profile;

public sealed class ProfileService(
    UserManager<ClubUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IProfileAvatarStorage avatarStorage,
    ClubDbContext db) : IProfileService
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, (string Extension, string ContentType)> AvatarTypes =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", "image/jpeg"),
            ["image/png"] = (".png", "image/png"),
            ["image/webp"] = (".webp", "image/webp")
        };
    public async Task<UserProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null || !user.IsActive ? null : await ToDtoAsync(user, cancellationToken);
    }

    public async Task<ProfileOperationResult> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return ProfileOperationResult.Failure("No se ha encontrado la cuenta.");

        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var preferredTeamId = request.PreferredTeamId;
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 80 || string.IsNullOrWhiteSpace(lastName) || lastName.Length > 79 || firstName.Length + lastName.Length + 1 > 160)
            return ProfileOperationResult.Failure("Indica un nombre (máximo 80 caracteres) y unos apellidos válidos; el nombre completo no puede superar 160 caracteres.");
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            return ProfileOperationResult.Failure("Introduce un correo electrónico válido.");
        if (preferredTeamId is not null && !await db.Teams.AnyAsync(
                team => team.Id == preferredTeamId && (team.IsActive || team.Id == user.PreferredTeamId), cancellationToken))
            return ProfileOperationResult.Failure("El equipo preferido seleccionado no existe o no está activo.");

        var normalizedEmail = userManager.NormalizeEmail(email);
        if (!string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
        {
            var existingUser = await userManager.Users.AsNoTracking().FirstOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail && candidate.Id != userId, cancellationToken);
            if (existingUser is not null) return ProfileOperationResult.Failure("Ese correo electrónico ya está asociado a otra cuenta.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        user.Email = email;
        user.UserName = email;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.DisplayName = $"{firstName} {lastName}".Trim();
        user.PreferredTeamId = preferredTeamId;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return ProfileOperationResult.Failure("No se pudo guardar la información del perfil.");

        await transaction.CommitAsync(cancellationToken);
        await RefreshSessionAsync(user);
        return ProfileOperationResult.Success(await ToDtoAsync(user, cancellationToken));
    }

    public async Task<ProfileOperationResult> SetAvatarAsync(Guid userId, string contentType, long length, Stream content, CancellationToken cancellationToken)
    {
        if (length <= 0 || length > MaxAvatarBytes) return ProfileOperationResult.Failure("La imagen debe pesar 5 MB o menos.");
        if (!AvatarTypes.TryGetValue(contentType, out var avatarType))
            return ProfileOperationResult.Failure("Formatos admitidos: JPEG, PNG y WebP.");
        if (!await HasValidSignatureAsync(content, avatarType.ContentType, cancellationToken))
            return ProfileOperationResult.Failure("El contenido del archivo no coincide con el formato de imagen indicado.");

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return ProfileOperationResult.Failure("No se ha encontrado la cuenta.");
        if (content.CanSeek) content.Position = 0;
        var oldStorageKey = user.AvatarStorageKey;
        var newStorageKey = await avatarStorage.SaveAsync(Guid.NewGuid(), avatarType.Extension, content, cancellationToken);
        user.AvatarStorageKey = newStorageKey;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await avatarStorage.DeleteAsync(newStorageKey, cancellationToken);
            return ProfileOperationResult.Failure("No se pudo guardar la foto de perfil.");
        }
        if (oldStorageKey is not null) await avatarStorage.DeleteAsync(oldStorageKey, cancellationToken);
        await RefreshSessionAsync(user);
        return ProfileOperationResult.Success(await ToDtoAsync(user, cancellationToken));
    }

    public async Task<bool> RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive || user.AvatarStorageKey is null) return false;
        var oldStorageKey = user.AvatarStorageKey;
        user.AvatarStorageKey = null;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return false;
        await avatarStorage.DeleteAsync(oldStorageKey, cancellationToken);
        await RefreshSessionAsync(user);
        return true;
    }

    public async Task<(Stream Content, string ContentType)?> OpenAvatarAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive || user.AvatarStorageKey is null) return null;
        var stream = await avatarStorage.OpenReadAsync(user.AvatarStorageKey, cancellationToken);
        if (stream is null) return null;
        var contentType = Path.GetExtension(user.AvatarStorageKey).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return (stream, contentType);
    }

    public async Task<Guid?> GetDefaultPreferredTeamIdAsync(CancellationToken cancellationToken) =>
        await db.Teams.AsNoTracking().Where(team => team.IsActive && team.Category == "Senior")
            .OrderBy(team => team.Name).Select(team => (Guid?)team.Id).FirstOrDefaultAsync(cancellationToken);

    private async Task<UserProfileDto> ToDtoAsync(ClubUser user, CancellationToken cancellationToken)
    {
        var availableTeams = await db.Teams.AsNoTracking().Where(team => team.IsActive || team.Id == user.PreferredTeamId)
            .OrderBy(team => team.Category).ThenBy(team => team.Name)
            .Select(team => new TeamPreferenceOptionDto(team.Id, team.Name, team.Category))
            .ToListAsync(cancellationToken);
        return new UserProfileDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.Email ?? string.Empty,
            user.PreferredTeamId, user.AvatarStorageKey is null ? null : $"/api/v1/profile/avatar/{user.Id}", availableTeams);
    }

    private async Task RefreshSessionAsync(ClubUser user)
    {
        var context = httpContextAccessor.HttpContext!;
        var currentAuthentication = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange((await userManager.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = currentAuthentication.Properties?.IsPersistent ?? false, AllowRefresh = true });
    }

    private static async Task<bool> HasValidSignatureAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        if (!content.CanSeek) return false;
        var header = new byte[12];
        var read = await content.ReadAsync(header.AsMemory(), cancellationToken);
        content.Position = 0;
        return contentType switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }
}
