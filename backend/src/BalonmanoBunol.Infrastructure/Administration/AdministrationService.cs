using BalonmanoBunol.Application.Administration;
using BalonmanoBunol.Domain.Auth;
using BalonmanoBunol.Domain.Administration;
using BalonmanoBunol.Domain.Teams;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BalonmanoBunol.Infrastructure.Administration;

public sealed class AdministrationService(ClubDbContext db, UserManager<ClubUser> users) : IAdministrationService
{
    private static readonly string[] EditableRoles = ["SocioAficion", "Jugador", "CuerpoTecnico", "Administrador"];

    public async Task<IReadOnlyList<AdminUserDto>> SearchUsersAsync(string query, bool coachesOnly, bool playersOnly, CancellationToken cancellationToken)
    {
        var term = query.Trim();
        if (!coachesOnly && term.Length < 2) return [];
        var usersQuery = db.Users.AsNoTracking().Where(user => user.IsActive);
        if (coachesOnly)
        {
            var coachIds = db.UserRoles.Join(db.Roles, userRole => userRole.RoleId, role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Where(item => item.Name == "CuerpoTecnico")
                .Select(item => item.UserId).Distinct();
            usersQuery = usersQuery.Where(user => coachIds.Contains(user.Id));
        }
        if (playersOnly)
        {
            var eligiblePlayerIds = db.UserRoles.Join(db.Roles, userRole => userRole.RoleId, role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Where(item => item.Name == "Jugador" || item.Name == "CuerpoTecnico")
                .Select(item => item.UserId).Distinct();
            var administratorIds = db.UserRoles.Join(db.Roles, userRole => userRole.RoleId, role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Where(item => item.Name == "Administrador")
                .Select(item => item.UserId).Distinct();
            usersQuery = usersQuery.Where(user => eligiblePlayerIds.Contains(user.Id) && !administratorIds.Contains(user.Id));
        }
        if (term.Length >= 2)
            usersQuery = usersQuery.Where(user => EF.Functions.ILike(user.DisplayName, $"%{EscapeLike(term)}%")
                || EF.Functions.ILike(user.Email!, $"%{EscapeLike(term)}%"));
        var matches = await usersQuery
            .OrderBy(user => user.DisplayName)
            .Take(coachesOnly ? 100 : 25)
            .ToListAsync(cancellationToken);
        var result = new List<AdminUserDto>(matches.Count);
        foreach (var user in matches)
        {
            var roleNames = await users.GetRolesAsync(user);
            var assignedTeams = roleNames.Contains("CuerpoTecnico")
                ? await db.TeamAssignments.AsNoTracking()
                    .Where(assignment => assignment.UserId == user.Id)
                    .Join(db.Teams.AsNoTracking(), assignment => assignment.TeamId, team => team.Id,
                        (_, team) => new { team.Id, team.Name, team.Category })
                    .OrderBy(team => team.Category).ThenBy(team => team.Name)
                    .Select(team => new AssignedTeamDto(team.Id, team.Name, team.Category))
                    .ToListAsync(cancellationToken)
                : [];
            result.Add(new AdminUserDto(user.Id, user.DisplayName, user.Email ?? string.Empty, roleNames.ToArray(), assignedTeams));
        }
        return result;
    }

    public async Task<IReadOnlyList<AdminTeamDto>> GetTeamsAsync(CancellationToken cancellationToken) =>
        await db.Teams.AsNoTracking().OrderBy(team => team.Category).ThenBy(team => team.Name)
            .Select(team => new AdminTeamDto(team.Id, team.Name, team.Category, team.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<AdminOperationResult> CreateTeamAsync(Guid administratorId, TeamInput input, CancellationToken cancellationToken)
    {
        var validation = ValidateTeam(input);
        if (validation is not null) return AdminOperationResult.Failure(validation);
        var team = new Team(input.Name, input.Category);
        db.Teams.Add(team);
        db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(administratorId, null, "TeamCreated", $"{team.Name} | {team.Category}"));
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> UpdateTeamAsync(Guid administratorId, Guid teamId, TeamInput input, CancellationToken cancellationToken)
    {
        var validation = ValidateTeam(input);
        if (validation is not null) return AdminOperationResult.Failure(validation);
        var team = await db.Teams.SingleOrDefaultAsync(item => item.Id == teamId, cancellationToken);
        if (team is null) return AdminOperationResult.Failure("No se ha encontrado el equipo.");
        team.Update(input.Name, input.Category);
        db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(administratorId, null, "TeamUpdated", $"{team.Id} | {team.Name} | {team.Category}"));
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> SetTeamActiveAsync(Guid administratorId, Guid teamId, bool isActive, CancellationToken cancellationToken)
    {
        var team = await db.Teams.SingleOrDefaultAsync(item => item.Id == teamId, cancellationToken);
        if (team is null) return AdminOperationResult.Failure("No se ha encontrado el equipo.");
        team.SetActive(isActive);
        db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(administratorId, null, isActive ? "TeamActivated" : "TeamDeactivated", team.Id.ToString()));
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> AssignTeamsAsync(Guid administratorId, Guid userId, AssignTeamsInput input, CancellationToken cancellationToken)
    {
        var target = await users.FindByIdAsync(userId.ToString());
        if (target is null || !target.IsActive) return AdminOperationResult.Failure("No se ha encontrado la cuenta.");
        var targetRoles = await users.GetRolesAsync(target);
        if (!targetRoles.Contains("CuerpoTecnico"))
            return AdminOperationResult.Failure("Solo las cuentas de Cuerpo Técnico pueden tener equipos asignados. Administración tiene acceso global.");
        var ids = input.TeamIds.Distinct().ToArray();
        if (ids.Length != input.TeamIds.Count) return AdminOperationResult.Failure("La selección contiene equipos repetidos.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var current = await db.TeamAssignments.Where(assignment => assignment.UserId == userId).ToListAsync(cancellationToken);
        var previousIds = current.Select(assignment => assignment.TeamId).Order().ToArray();
        var currentIds = current.Select(assignment => assignment.TeamId).ToArray();
        var eligibleIds = await db.Teams.Where(team => ids.Contains(team.Id)
                && (team.IsActive || currentIds.Contains(team.Id)))
            .Select(team => team.Id).ToListAsync(cancellationToken);
        if (eligibleIds.Count != ids.Length) return AdminOperationResult.Failure("Uno o varios equipos nuevos no existen o están inactivos.");

        db.TeamAssignments.RemoveRange(current);
        foreach (var teamId in ids) db.TeamAssignments.Add(new TeamAssignment(userId, teamId, administratorId));
        var action = ids.Length == 0 ? "TeamsUnassigned" : "TeamsAssigned";
        var details = ids.Length == 0
            ? $"Retiradas todas las asignaciones: {string.Join(',', previousIds)}"
            : string.Join(',', ids.Order());
        db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(administratorId, userId, action, details));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<IReadOnlyList<AdminTeamPlayerDto>> GetTeamPlayersAsync(Guid teamId, CancellationToken cancellationToken) =>
        await db.TeamPlayerMemberships.AsNoTracking().Where(membership => membership.TeamId == teamId)
            .Join(db.Users.AsNoTracking(), membership => membership.UserId, player => player.Id,
                (membership, player) => new { player.Id, player.DisplayName, Email = player.Email ?? string.Empty, membership.JerseyNumber, membership.Position })
            .OrderBy(player => player.JerseyNumber == null).ThenBy(player => player.JerseyNumber).ThenBy(player => player.DisplayName)
            .Select(player => new AdminTeamPlayerDto(player.Id, player.DisplayName, player.Email, player.JerseyNumber, player.Position))
            .ToListAsync(cancellationToken);

    public async Task<AdminOperationResult> AssignPlayerAsync(Guid administratorId, Guid userId, AdminPlayerAssignmentInput input, CancellationToken cancellationToken)
    {
        var administrator = await users.FindByIdAsync(administratorId.ToString());
        if (administrator is null || !await users.IsInRoleAsync(administrator, "Administrador"))
            return AdminOperationResult.Failure("Solo Administración puede asignar jugadores globalmente.");
        var target = await users.FindByIdAsync(userId.ToString());
        if (target is null || !target.IsActive || !await IsEligiblePlayerAsync(target))
            return AdminOperationResult.Failure("Selecciona una cuenta activa con rol Jugador o Cuerpo Técnico.");
        if (input.JerseyNumber is < 0 or > 99 || string.IsNullOrWhiteSpace(input.Position) || input.Position.Trim().Length > 80)
            return AdminOperationResult.Failure("Revisa el dorsal y la posición del jugador.");
        if (input.JerseyNumber is not null && await db.TeamPlayerMemberships.AnyAsync(item => item.TeamId == input.TeamId && item.JerseyNumber == input.JerseyNumber && item.UserId != userId, cancellationToken))
            return AdminOperationResult.Failure("Ese dorsal ya está asignado en este equipo.");
        var team = await db.Teams.SingleOrDefaultAsync(item => item.Id == input.TeamId && item.IsActive, cancellationToken);
        if (team is null) return AdminOperationResult.Failure("No se ha encontrado un equipo activo.");
        var membership = await db.TeamPlayerMemberships.SingleOrDefaultAsync(item => item.TeamId == input.TeamId && item.UserId == userId, cancellationToken);
        if (membership is null) db.TeamPlayerMemberships.Add(new TeamPlayerMembership(input.TeamId, userId, administratorId, input.JerseyNumber, input.Position));
        else membership.Update(input.JerseyNumber, input.Position, membership.MatchesPlayed, membership.MinutesPlayed, membership.Goals, membership.Saves);
        db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(administratorId, userId, "PlayerAssigned", $"{userId} -> {team.Name} | #{input.JerseyNumber} | {input.Position}"));
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> AddPlayerByStaffAsync(Guid staffId, Guid teamId, Guid playerId, int? jerseyNumber, string position, CancellationToken cancellationToken)
    {
        if (!await IsStaffAssignedAsync(staffId, teamId, cancellationToken))
            return AdminOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        return await UpsertPlayerMembershipAsync(staffId, playerId, teamId, jerseyNumber, position, cancellationToken);
    }

    private async Task<AdminOperationResult> UpsertPlayerMembershipAsync(Guid actorId, Guid playerId, Guid teamId, int? jerseyNumber, string position, CancellationToken cancellationToken)
    {
        if (jerseyNumber is < 0 or > 99 || string.IsNullOrWhiteSpace(position) || position.Trim().Length > 80)
            return AdminOperationResult.Failure("Revisa el dorsal y la posición del jugador.");
        var jerseyTaken = jerseyNumber is not null && await db.TeamPlayerMemberships.AnyAsync(item => item.TeamId == teamId && item.JerseyNumber == jerseyNumber && item.UserId != playerId, cancellationToken);
        if (jerseyTaken) return AdminOperationResult.Failure("Ese dorsal ya está asignado en este equipo.");
        var target = await users.FindByIdAsync(playerId.ToString());
        if (target is null || !target.IsActive || !await IsEligiblePlayerAsync(target))
            return AdminOperationResult.Failure("Selecciona una cuenta activa con rol Jugador o Cuerpo Técnico.");
        var team = await db.Teams.SingleOrDefaultAsync(item => item.Id == teamId && item.IsActive, cancellationToken);
        if (team is null) return AdminOperationResult.Failure("No se ha encontrado un equipo activo.");
        var membership = await db.TeamPlayerMemberships.SingleOrDefaultAsync(item => item.TeamId == teamId && item.UserId == playerId, cancellationToken);
        if (membership is null)
            db.TeamPlayerMemberships.Add(new TeamPlayerMembership(teamId, playerId, actorId, jerseyNumber, position));
        else
            membership.Update(jerseyNumber, position, membership.MatchesPlayed, membership.MinutesPlayed, membership.Goals, membership.Saves);
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> RemovePlayerByStaffAsync(Guid staffId, Guid teamId, Guid playerId, CancellationToken cancellationToken)
    {
        if (!await IsStaffAssignedAsync(staffId, teamId, cancellationToken))
            return AdminOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        var membership = await db.TeamPlayerMemberships.SingleOrDefaultAsync(item => item.TeamId == teamId && item.UserId == playerId, cancellationToken);
        if (membership is null) return AdminOperationResult.Failure("El jugador no pertenece a este equipo.");
        db.TeamPlayerMemberships.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> RemovePlayerFromRosterAsync(Guid actorId, Guid playerId, Guid teamId, CancellationToken cancellationToken)
    {
        var actor = await users.FindByIdAsync(actorId.ToString());
        var isAdmin = actor is not null && await users.IsInRoleAsync(actor, "Administrador");
        if (!isAdmin)
            return AdminOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        var membership = await db.TeamPlayerMemberships.SingleOrDefaultAsync(item => item.TeamId == teamId && item.UserId == playerId, cancellationToken);
        if (membership is null) return AdminOperationResult.Failure("El jugador no pertenece a ese equipo.");
        db.TeamPlayerMemberships.Remove(membership);
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> RemovePlayerAsync(Guid administratorId, Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        var administrator = await users.FindByIdAsync(administratorId.ToString());
        if (administrator is null || !await users.IsInRoleAsync(administrator, "Administrador"))
            return AdminOperationResult.Failure("Solo Administración puede retirar jugadores globalmente.");
        var membership = await db.TeamPlayerMemberships.SingleOrDefaultAsync(item => item.UserId == userId && item.TeamId == teamId, cancellationToken);
        if (membership is null) return AdminOperationResult.Failure("El jugador no pertenece a ese equipo.");
        db.TeamPlayerMemberships.Remove(membership);
        db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(administratorId, userId, "PlayerRemoved", $"{userId} <- {teamId}"));
        await db.SaveChangesAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    public async Task<AdminOperationResult> ChangeRoleAsync(Guid administratorId, Guid userId, ChangeRoleInput input, CancellationToken cancellationToken)
    {
        if (!EditableRoles.Contains(input.Role)) return AdminOperationResult.Failure("El rol seleccionado no es válido.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockAdministratorRoleAsync(transaction.GetDbTransaction(), cancellationToken);

        var target = await users.FindByIdAsync(userId.ToString());
        var administrator = await users.FindByIdAsync(administratorId.ToString());
        if (target is null || !target.IsActive || administrator is null) return AdminOperationResult.Failure("No se ha encontrado la cuenta.");
        var targetRoles = await users.GetRolesAsync(target);
        var isChangingAdmin = targetRoles.Contains("Administrador") != (input.Role == "Administrador");
        if (isChangingAdmin && administratorId == userId)
            return AdminOperationResult.Failure("No puedes cambiar tu propio rol de Administrador.");
        if (isChangingAdmin && !await users.IsInRoleAsync(administrator, "Administrador"))
            return AdminOperationResult.Failure("Solo otro Administrador puede cambiar el rol de Administrador.");
        if (targetRoles.Contains("Administrador") && input.Role != "Administrador")
        {
            var activeAdmins = await db.UserRoles.Join(db.Roles, userRole => userRole.RoleId, role => role.Id,
                    (userRole, role) => new { userRole.UserId, role.Name })
                .Join(db.Users, role => role.UserId, user => user.Id, (role, user) => new { role.Name, user.IsActive })
                .CountAsync(item => item.IsActive && item.Name == "Administrador", cancellationToken);
            if (activeAdmins <= 1) return AdminOperationResult.Failure("Debe permanecer al menos un Administrador activo.");
        }

        var pendingRoleRequests = await db.RoleRequests.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == RoleRequestStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var request in pendingRoleRequests)
        {
            var matchesRequestedRole = request.RequestedRole == input.Role;
            var resolvedAt = DateTimeOffset.UtcNow;
            var resolvedStatus = matchesRequestedRole ? RoleRequestStatus.Approved : RoleRequestStatus.Rejected;
            var reserved = await db.RoleRequests
                .Where(item => item.Id == request.Id && item.Status == RoleRequestStatus.Pending)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(item => item.Status, resolvedStatus)
                    .SetProperty(item => item.ResolvedByUserId, administratorId)
                    .SetProperty(item => item.ResolvedAt, resolvedAt), cancellationToken);
            if (reserved != 1)
                return AdminOperationResult.Conflict("Una solicitud de rol de esta cuenta ya ha sido resuelta.");
        }

        var oldPrincipalRoles = targetRoles.Where(role => EditableRoles.Contains(role)).ToArray();
        var removeResult = await users.RemoveFromRolesAsync(target, oldPrincipalRoles);
        if (!removeResult.Succeeded) return AdminOperationResult.Failure("No se pudieron actualizar los roles de la cuenta.");
        var addResult = await users.AddToRoleAsync(target, input.Role);
        if (!addResult.Succeeded) return AdminOperationResult.Failure("No se pudieron actualizar los roles de la cuenta.");
        db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(administratorId, userId, "RoleChanged", $"{string.Join(',', oldPrincipalRoles)} -> {input.Role}"));

        foreach (var request in pendingRoleRequests)
        {
            var matchesRequestedRole = request.RequestedRole == input.Role;
            db.AdministrationAuditEntries.Add(new AdministrationAuditEntry(
                administratorId,
                userId,
                matchesRequestedRole ? "RoleRequestApprovedByManualChange" : "RoleRequestRejectedByManualChange",
                $"{request.Id} | {request.RequestedRole} -> {input.Role}"));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AdminOperationResult.Success();
    }

    private async Task LockAdministratorRoleAsync(System.Data.Common.DbTransaction transaction, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // Serialize every role mutation on the shared Administrador role row so the
        // active-admin count and the subsequent role update cannot race each other.
        command.CommandText = "SELECT \"Id\" FROM \"AspNetRoles\" WHERE \"NormalizedName\" = @roleName FOR UPDATE";
        var roleName = command.CreateParameter();
        roleName.ParameterName = "roleName";
        roleName.Value = "ADMINISTRADOR";
        command.Parameters.Add(roleName);

        var roleId = await command.ExecuteScalarAsync(cancellationToken);
        if (roleId is null or DBNull) throw new InvalidOperationException("No se ha encontrado el rol Administrador para bloquear cambios de rol.");
    }

    private static string? ValidateTeam(TeamInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 120) return "El nombre del equipo es obligatorio y no puede superar 120 caracteres.";
        if (string.IsNullOrWhiteSpace(input.Category) || input.Category.Trim().Length > 80) return "La categoría del equipo es obligatoria.";
        if (input.Category.Trim() is not ("Benjamín" or "Alevín" or "Infantil" or "Cadete" or "Juvenil" or "Senior")) return "La categoría del equipo no es válida.";
        return null;
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private async Task<bool> IsEligiblePlayerAsync(ClubUser target) =>
        (await users.IsInRoleAsync(target, "Jugador") || await users.IsInRoleAsync(target, "CuerpoTecnico"))
        && !await users.IsInRoleAsync(target, "Administrador");

    private async Task<bool> IsStaffAssignedAsync(Guid staffId, Guid teamId, CancellationToken cancellationToken)
    {
        var staff = await users.FindByIdAsync(staffId.ToString());
        return staff is { IsActive: true }
            && await users.IsInRoleAsync(staff, "CuerpoTecnico")
            && await db.TeamAssignments.AnyAsync(assignment => assignment.UserId == staffId && assignment.TeamId == teamId, cancellationToken);
    }
}
