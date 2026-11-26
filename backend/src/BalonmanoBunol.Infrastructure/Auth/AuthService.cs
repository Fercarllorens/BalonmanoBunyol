using System.Security.Claims;
using BalonmanoBunol.Application.Auth;
using BalonmanoBunol.Application.Profile;
using BalonmanoBunol.Domain.Administration;
using BalonmanoBunol.Domain.Auth;
using BalonmanoBunol.Domain.Teams;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BalonmanoBunol.Infrastructure.Auth;

public sealed class AuthService(
    ClubDbContext dbContext,
    UserManager<ClubUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IOptions<IdentityOptions> identityOptions,
    IProfileService profiles) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = request.Email.Trim();
        var accountType = request.AccountType.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 80
            || string.IsNullOrWhiteSpace(lastName) || lastName.Length > 79
            || firstName.Length + lastName.Length + 1 > 160
            || string.IsNullOrWhiteSpace(email))
            return AuthResult.Failure("Completa todos los campos obligatorios.");
        if (accountType is not ("Socio" or "Jugador" or "CuerpoTecnico"))
            return AuthResult.Failure("El tipo de alta no es válido.");
        if (await userManager.FindByEmailAsync(email) is not null)
            return AuthResult.Failure("No se ha podido completar el registro con esos datos.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ClubUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = $"{firstName} {lastName}".Trim(),
            FirstName = firstName,
            LastName = lastName,
            PreferredTeamId = await profiles.GetDefaultPreferredTeamIdAsync(cancellationToken),
            IsActive = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AuthResult.Failure(FormatIdentityErrors(createResult));
        }

        var roleResult = await userManager.AddToRoleAsync(user, "SocioAficion");
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AuthResult.Failure("No se ha podido completar el registro.");
        }

        if (accountType is "Jugador" or "CuerpoTecnico")
        {
            dbContext.RoleRequests.Add(new RoleRequest(user.Id, accountType));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        var userDto = await MapUserAsync(user, cancellationToken);
        if (userDto is not null) await SignInAsync(user, isPersistent: false);
        return userDto is null ? AuthResult.Failure("No se ha podido completar el registro.") : AuthResult.Success(userDto);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
            return AuthResult.Failure("El correo o la contraseña no son correctos.");

        if (await userManager.IsLockedOutAsync(user))
            return AuthResult.Failure("El correo o la contraseña no son correctos.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthResult.Failure("El correo o la contraseña no son correctos.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await SignInAsync(user, request.RememberMe);
        var userDto = await MapUserAsync(user, cancellationToken);
        return userDto is null ? AuthResult.Failure("No se ha podido iniciar sesión.") : AuthResult.Success(userDto);
    }

    public async Task<AuthUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            await httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return null;
        }

        var currentAuthentication = await httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SignInAsync(user, currentAuthentication.Properties?.IsPersistent ?? false);
        return await MapUserAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<IReadOnlyList<RoleRequestDto>> GetRoleRequestsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RoleRequests.AsNoTracking()
            .OrderBy(request => request.Status)
            .ThenBy(request => request.CreatedAt)
            .Join(dbContext.Users.AsNoTracking(), request => request.UserId, user => user.Id,
                (request, user) => new RoleRequestDto(
                    request.Id,
                    request.UserId,
                    user.DisplayName,
                    user.Email ?? string.Empty,
                    request.RequestedRole,
                    request.Status.ToString(),
                    request.CreatedAt,
                    request.ResolvedAt,
                    request.ResolvedByUserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ResolveRoleRequestAsync(Guid requestId, Guid administratorId, bool approve, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var request = await dbContext.RoleRequests.AsNoTracking()
            .Where(item => item.Id == requestId)
            .Select(item => new { item.Id, item.UserId, item.RequestedRole })
            .SingleOrDefaultAsync(cancellationToken);
        if (request is null) return false;

        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) return false;

        var resolvedAt = DateTimeOffset.UtcNow;
        var status = approve ? RoleRequestStatus.Approved : RoleRequestStatus.Rejected;
        var reserved = await dbContext.RoleRequests
            .Where(item => item.Id == requestId && item.Status == RoleRequestStatus.Pending)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.Status, status)
                .SetProperty(item => item.ResolvedByUserId, administratorId)
                .SetProperty(item => item.ResolvedAt, resolvedAt), cancellationToken);
        if (reserved != 1) return false;

        if (approve)
        {
            var oldRoles = await userManager.GetRolesAsync(user);
            var principalRoles = new[] { "SocioAficion", "Jugador", "CuerpoTecnico" };
            foreach (var oldRole in oldRoles.Where(role => principalRoles.Contains(role) && role != request.RequestedRole))
            {
                var removeResult = await userManager.RemoveFromRoleAsync(user, oldRole);
                if (!removeResult.Succeeded) return false;
            }
            if (!await userManager.IsInRoleAsync(user, request.RequestedRole))
            {
                var addResult = await userManager.AddToRoleAsync(user, request.RequestedRole);
                if (!addResult.Succeeded) return false;
            }
        }

        dbContext.AdministrationAuditEntries.Add(new AdministrationAuditEntry(
            administratorId,
            user.Id,
            approve ? "RoleRequestApproved" : "RoleRequestRejected",
            $"{request.Id} | {request.RequestedRole}"));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<AuthUserDto?> MapUserAsync(ClubUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roles = await userManager.GetRolesAsync(user);
        return new AuthUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.ToArray(),
            user.FirstName,
            user.LastName,
            user.PreferredTeamId,
            user.AvatarStorageKey is null ? null : $"/api/v1/profile/avatar/{user.Id}");
    }

    private async Task SignInAsync(ClubUser user, bool isPersistent)
    {
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = isPersistent, AllowRefresh = true });
    }

    private string FormatIdentityErrors(IdentityResult result)
    {
        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            return "No se ha podido completar el registro con esos datos.";

        var errors = result.Errors.Select(error => error.Code switch
        {
            "PasswordTooShort" => $"La contraseña debe tener al menos {identityOptions.Value.Password.RequiredLength} caracteres.",
            "PasswordRequiresDigit" => "La contraseña debe contener al menos un número.",
            "PasswordRequiresUpper" => "La contraseña debe contener al menos una letra mayúscula.",
            "PasswordRequiresLower" => "La contraseña debe contener al menos una letra minúscula.",
            "PasswordRequiresNonAlphanumeric" => "La contraseña debe contener al menos un símbolo.",
            _ => error.Description
        });
        return string.Join(" ", errors);
    }
}
