using System.Security.Cryptography;
using BalonmanoBunol.Application.Administration;
using BalonmanoBunol.Application.Teams;
using BalonmanoBunol.Domain.Teams;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BalonmanoBunol.Infrastructure.Teams;

public sealed class TeamSpaceService(ClubDbContext db, UserManager<ClubUser> users, ITeamBoardStorage storage, IAdministrationService administration) : ITeamSpaceService
{
    private static readonly TimeZoneInfo ClubTimeZone = ResolveClubTimeZone();

    private static TimeZoneInfo ResolveClubTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"); }
    }

    private static DateOnly ClubToday() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ClubTimeZone).DateTime);
    private static DateTimeOffset ClubDateTime(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, ClubTimeZone.GetUtcOffset(local));
    }

    public async Task<IReadOnlyList<TeamSummaryDto>> GetMyTeamsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return [];
        if (await users.IsInRoleAsync(user, "Administrador"))
            return await db.Teams.AsNoTracking().Where(team => team.IsActive)
                .OrderBy(team => team.Category).ThenBy(team => team.Name)
                .Select(team => new TeamSummaryDto(team.Id, team.Name, team.Category, "Administrador"))
                .ToListAsync(cancellationToken);

        var staff = await db.TeamAssignments.AsNoTracking().Where(item => item.UserId == userId)
            .Join(db.Teams.AsNoTracking().Where(team => team.IsActive), item => item.TeamId, team => team.Id,
                (_, team) => new TeamSummaryDto(team.Id, team.Name, team.Category, "CuerpoTecnico"))
            .ToListAsync(cancellationToken);
        var playerTeams = await db.TeamPlayerMemberships.AsNoTracking().Where(item => item.UserId == userId)
            .Join(db.Teams.AsNoTracking().Where(team => team.IsActive), item => item.TeamId, team => team.Id,
                (_, team) => new TeamSummaryDto(team.Id, team.Name, team.Category, "Jugador"))
            .ToListAsync(cancellationToken);
        return staff.Concat(playerTeams).DistinctBy(team => team.Id).OrderBy(team => team.Category).ThenBy(team => team.Name).ToArray();
    }

    public async Task<TeamSpaceDto?> GetTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        var team = await db.Teams.AsNoTracking().SingleOrDefaultAsync(item => item.Id == teamId && item.IsActive, cancellationToken);
        if (team is null) return null;
        var access = await GetAccessAsync(userId, teamId, cancellationToken);
        if (access is null) return null;

        var players = await db.TeamPlayerMemberships.AsNoTracking().Where(item => item.TeamId == teamId)
            .Join(db.Users.AsNoTracking(), item => item.UserId, player => player.Id,
                (membership, player) => new { player.Id, player.DisplayName, Email = player.Email ?? string.Empty,
                    AvatarStorageKey = player.AvatarStorageKey, membership.JerseyNumber, membership.Position,
                    membership.MatchesPlayed, membership.MinutesPlayed, membership.Goals, membership.Saves })
            .OrderBy(item => item.JerseyNumber == null).ThenBy(item => item.JerseyNumber).ThenBy(item => item.DisplayName)
            .Select(item => new TeamPlayerDto(item.Id, item.DisplayName, item.Email,
                item.AvatarStorageKey == null ? null : $"/api/v1/profile/avatar/{item.Id}", item.JerseyNumber,
                item.Position, item.MatchesPlayed, item.MinutesPlayed, item.Goals, item.Saves))
            .ToListAsync(cancellationToken);

        var today = ClubToday();
        var todayStart = ClubDateTime(today, TimeOnly.MinValue).ToUniversalTime();
        var matches = await db.TeamMatches.AsNoTracking()
            .Where(match => match.TeamId == teamId && match.IsActive && match.StartsAt >= todayStart)
            .OrderBy(match => match.StartsAt).Take(20).ToListAsync(cancellationToken);
        var matchIds = matches.Select(match => match.Id).ToArray();
        var callups = await (from item in db.TeamCallups.AsNoTracking()
                             join player in db.Users.AsNoTracking() on item.PlayerId equals player.Id
                             join membership in db.TeamPlayerMemberships.AsNoTracking() on new { TeamId = teamId, UserId = player.Id } equals new { membership.TeamId, membership.UserId }
                             where matchIds.Contains(item.MatchId)
                             select new { item.MatchId, Player = new TeamCallupDto(player.Id, player.DisplayName, membership.JerseyNumber, membership.Position, item.Attendance) })
            .ToListAsync(cancellationToken);
        var matchDtos = matches.Select(match => new TeamMatchDto(match.Id, match.Opponent, match.StartsAt, match.Venue, match.Round,
            callups.Where(callup => callup.MatchId == match.Id).Select(callup => callup.Player).OrderBy(player => player.JerseyNumber).ToArray())).ToArray();
        var events = await db.TeamEvents.AsNoTracking().Where(item => item.TeamId == teamId && !item.IsCancelled)
            .OrderBy(item => item.StartsAt).Take(40)
            .Select(item => new TeamEventDto(item.Id, item.Title, item.Kind, item.StartsAt, item.EndsAt, item.Venue, item.Notes, access == "CuerpoTecnico" || access == "Administrador"))
            .ToListAsync(cancellationToken);
        var boardPosts = await db.TeamBoardPosts.AsNoTracking().Where(post => post.TeamId == teamId)
            .OrderByDescending(post => post.CreatedAt).Take(30).ToListAsync(cancellationToken);
        var authorIds = boardPosts.Select(post => post.AuthorId).Distinct().ToArray();
        var authors = await db.Users.AsNoTracking().Where(author => authorIds.Contains(author.Id))
            .ToDictionaryAsync(author => author.Id, author => author.DisplayName, cancellationToken);
        var boardIds = boardPosts.Select(post => post.Id).ToArray();
        var attachments = await db.TeamBoardAttachments.AsNoTracking().Where(item => boardIds.Contains(item.BoardPostId)).ToListAsync(cancellationToken);
        var boardDtos = boardPosts.Select(post => new TeamBoardPostDto(post.Id, post.AuthorId, authors.GetValueOrDefault(post.AuthorId, "Usuario del club"),
            post.Content, post.CreatedAt, attachments.Where(item => item.BoardPostId == post.Id)
                .Select(item => new TeamBoardAttachmentDto(item.Id, item.FileName, item.ContentType, item.Length, $"/api/v1/teams/{teamId}/board-files/{item.Id}"))
                .ToArray())).ToArray();
        return new TeamSpaceDto(new TeamSummaryDto(team.Id, team.Name, team.Category, access), players, matchDtos, events, boardDtos, access is "CuerpoTecnico" or "Administrador");
    }

    public async Task<TeamCalendarDto?> GetCalendarAsync(Guid userId, Guid teamId, int year, int month, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(userId, teamId, cancellationToken);
        if (access is null) return null;
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var today = ClubToday();
        var seasonStartYear = today.Month >= 7 ? today.Year : today.Year - 1;
        var seasonStart = new DateOnly(seasonStartYear, 7, 1);
        var seasonEnd = new DateOnly(seasonStartYear + 1, 6, 30);
        var selectedMonth = new DateOnly(year, month, 1);
        if (selectedMonth < seasonStart || selectedMonth > seasonEnd) selectedMonth = new DateOnly(today.Year, today.Month, 1);
        var monthStart = selectedMonth;
        var monthEnd = selectedMonth.AddMonths(1).AddDays(-1);
        var monthStartInstant = ClubDateTime(monthStart, TimeOnly.MinValue);
        var monthEndInstant = ClubDateTime(monthEnd.AddDays(1), TimeOnly.MinValue);

        var plans = await db.TeamWeeklyTrainingPlans.AsNoTracking().Include(item => item.Slots)
            .Where(item => item.TeamId == teamId && item.EffectiveFrom >= seasonStart && item.EffectiveFrom <= seasonEnd)
            .OrderBy(item => item.EffectiveFrom).ToListAsync(cancellationToken);
        var planDtos = access is "CuerpoTecnico" or "Administrador"
            ? plans.Select(plan => new TeamTrainingPlanDto(plan.EffectiveFrom,
                plan.Slots.OrderBy(slot => slot.DayOfWeek).ThenBy(slot => slot.StartsAt)
                    .Select(slot => new TeamTrainingSlotDto(slot.Id, slot.DayOfWeek, slot.StartsAt, slot.EndsAt, slot.Venue)).ToArray())).ToArray()
            : [];
        var allSlots = plans.SelectMany(plan => plan.Slots.Select(slot => (Plan: plan, Slot: slot))).ToArray();
        var slotIds = allSlots.Select(item => item.Slot.Id).ToArray();
        var exceptions = await db.TeamTrainingExceptions.AsNoTracking()
            .Where(item => slotIds.Contains(item.SlotId) && item.Date >= seasonStart && item.Date <= monthEnd)
            .ToDictionaryAsync(item => (item.SlotId, item.Date), cancellationToken);

        var calendarItems = new List<TeamCalendarItemDto>();
        for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
        {
            var effectivePlan = plans.LastOrDefault(plan => plan.EffectiveFrom <= date);
            if (effectivePlan is null || effectivePlan.IsEmpty) continue;
            foreach (var slot in effectivePlan.Slots.Where(item => item.DayOfWeek == date.DayOfWeek))
            {
                var exceptionForDate = exceptions.GetValueOrDefault((slot.Id, date));
                var exception = exceptionForDate;
                if (exception?.IsCancelled == true)
                {
                    calendarItems.Add(new TeamCalendarItemDto($"training:{slot.Id}:{date:yyyyMMdd}", "Planificacion", "Entrenamiento",
                        "Entrenamiento cancelado", ClubDateTime(date, slot.StartsAt), null,
                        slot.Venue, string.Empty, IsCancelled: true, TrainingSlotId: slot.Id, TrainingDate: date));
                    continue;
                }
                var startTime = exception?.StartsAt ?? slot.StartsAt;
                var endTime = exception?.EndsAt ?? slot.EndsAt;
                var start = ClubDateTime(date, startTime);
                var end = ClubDateTime(date, endTime);
                calendarItems.Add(new TeamCalendarItemDto($"training:{slot.Id}:{date:yyyyMMdd}", "Planificacion", "Entrenamiento",
                    "Entrenamiento", start, end, exception?.Venue ?? slot.Venue, string.Empty,
                    TrainingSlotId: slot.Id, TrainingDate: date));
            }
        }

        var events = await db.TeamEvents.AsNoTracking().Where(item => item.TeamId == teamId && !item.IsCancelled)
            .OrderBy(item => item.StartsAt).ToListAsync(cancellationToken);
        events = events.Where(item => item.StartsAt >= monthStartInstant && item.StartsAt < monthEndInstant).ToList();
        calendarItems.AddRange(events.Select(item => new TeamCalendarItemDto(item.Id.ToString(), "Evento", item.Kind,
            item.Title, item.StartsAt, item.EndsAt, item.Venue, item.Notes, CanManage: access is "CuerpoTecnico" or "Administrador")));

        var matches = await db.TeamMatches.AsNoTracking().Where(item => item.TeamId == teamId && item.IsActive)
            .OrderBy(item => item.StartsAt).ToListAsync(cancellationToken);
        matches = matches.Where(item => item.StartsAt >= monthStartInstant && item.StartsAt < monthEndInstant).ToList();
        var calendarMatchIds = matches.Select(match => match.Id).ToArray();
        var callups = await (from callup in db.TeamCallups.AsNoTracking()
                             join player in db.Users.AsNoTracking() on callup.PlayerId equals player.Id
                             join membership in db.TeamPlayerMemberships.AsNoTracking() on new { TeamId = teamId, UserId = player.Id } equals new { membership.TeamId, membership.UserId }
                             where calendarMatchIds.Contains(callup.MatchId)
                             select new { callup.MatchId, PlayerId = player.Id, player.DisplayName, membership.JerseyNumber, membership.Position, callup.Attendance })
            .ToListAsync(cancellationToken);
        foreach (var match in matches)
        {
            var item = new TeamCalendarItemDto(match.Id.ToString(), "Partido", "Partido", match.Opponent, match.StartsAt, null,
                match.Venue, match.Round, CanManage: access is "CuerpoTecnico" or "Administrador",
                Callups: callups.Where(callup => callup.MatchId == match.Id)
                    .OrderBy(callup => callup.JerseyNumber)
                    .Select(callup => new TeamCallupDto(callup.PlayerId, callup.DisplayName, callup.JerseyNumber, callup.Position, callup.Attendance)).ToArray());
            calendarItems.Add(item);
        }

        return new TeamCalendarDto(seasonStart, seasonEnd, selectedMonth,
            calendarItems.OrderBy(item => item.StartsAt).ToArray(), planDtos);
    }

    public async Task<IReadOnlyList<TeamPlayerSearchDto>> SearchPlayersAsync(Guid userId, Guid teamId, string query, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return [];
        var term = query.Trim();
        if (term.Length < 2) return [];
        var assignedIds = db.TeamPlayerMemberships.Where(item => item.TeamId == teamId).Select(item => item.UserId);
        return await db.Users.AsNoTracking().Where(player => player.IsActive && !assignedIds.Contains(player.Id)
                && db.UserRoles.Join(db.Roles, item => item.RoleId, role => role.Id, (item, role) => new { item.UserId, role.Name })
                    .Any(role => role.UserId == player.Id && (role.Name == "Jugador" || role.Name == "CuerpoTecnico"))
                && !db.UserRoles.Join(db.Roles, item => item.RoleId, role => role.Id, (item, role) => new { item.UserId, role.Name })
                    .Any(role => role.UserId == player.Id && role.Name == "Administrador")
                && (EF.Functions.ILike(player.DisplayName, $"%{EscapeLike(term)}%") || EF.Functions.ILike(player.Email!, $"%{EscapeLike(term)}%")))
            .OrderBy(player => player.DisplayName).Take(20)
            .Select(player => new TeamPlayerSearchDto(player.Id, player.DisplayName, player.Email ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<TeamOperationResult> AddPlayerAsync(Guid userId, Guid teamId, TeamPlayerInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        if (await IsAdministratorAsync(userId))
        {
            var adminResult = await administration.AssignPlayerAsync(userId, input.UserId,
                new AdminPlayerAssignmentInput(teamId, input.JerseyNumber, input.Position), cancellationToken);
            return adminResult.Succeeded ? TeamOperationResult.Success() : TeamOperationResult.Failure(adminResult.Error ?? "No se pudo añadir al jugador.");
        }
        var result = await administration.AddPlayerByStaffAsync(userId, teamId, input.UserId, input.JerseyNumber, input.Position, cancellationToken);
        return result.Succeeded ? TeamOperationResult.Success() : TeamOperationResult.Failure(result.Error ?? "No se pudo añadir al jugador.");
    }

    public async Task<TeamOperationResult> RemovePlayerAsync(Guid userId, Guid teamId, Guid playerId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        if (await IsAdministratorAsync(userId))
        {
            var adminResult = await administration.RemovePlayerAsync(userId, playerId, teamId, cancellationToken);
            return adminResult.Succeeded ? TeamOperationResult.Success() : TeamOperationResult.Failure(adminResult.Error ?? "No se pudo retirar al jugador.");
        }
        var result = await administration.RemovePlayerByStaffAsync(userId, teamId, playerId, cancellationToken);
        return result.Succeeded ? TeamOperationResult.Success() : TeamOperationResult.Failure(result.Error ?? "No se pudo retirar al jugador.");
    }

    public async Task<TeamOperationResult> UpdatePlayerAsync(Guid userId, Guid teamId, TeamPlayerInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        var membership = await db.TeamPlayerMemberships.SingleOrDefaultAsync(item => item.TeamId == teamId && item.UserId == input.UserId, cancellationToken);
        if (membership is null) return TeamOperationResult.Failure("El jugador no pertenece a este equipo.");
        membership.Update(input.JerseyNumber, input.Position, membership.MatchesPlayed, membership.MinutesPlayed, membership.Goals, membership.Saves);
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> UpdatePlayerStatsAsync(Guid userId, Guid teamId, TeamPlayerStatsInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para editar estadísticas.");
        var membership = await db.TeamPlayerMemberships.SingleOrDefaultAsync(item => item.TeamId == teamId && item.UserId == input.UserId, cancellationToken);
        if (membership is null) return TeamOperationResult.Failure("El jugador no pertenece a este equipo.");
        try { membership.Update(membership.JerseyNumber, membership.Position, input.MatchesPlayed, input.MinutesPlayed, input.Goals, input.Saves); }
        catch (ArgumentOutOfRangeException) { return TeamOperationResult.Failure("Las estadísticas deben ser valores no negativos."); }
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<(TeamOperationResult Result, Guid? MatchId)> CreateMatchAsync(Guid userId, Guid teamId, TeamMatchInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return (TeamOperationResult.Failure("No tienes permisos para gestionar este equipo."), null);
        if (string.IsNullOrWhiteSpace(input.Opponent) || string.IsNullOrWhiteSpace(input.Venue)) return (TeamOperationResult.Failure("Completa rival y pabellón."), null);
        var playerIds = input.CalledUpPlayerIds.Distinct().ToArray();
        if (playerIds.Length != input.CalledUpPlayerIds.Count) return (TeamOperationResult.Failure("La convocatoria contiene jugadores repetidos."), null);
        var memberIds = await db.TeamPlayerMemberships.Where(item => item.TeamId == teamId && playerIds.Contains(item.UserId)).Select(item => item.UserId).ToListAsync(cancellationToken);
        if (memberIds.Count != playerIds.Length) return (TeamOperationResult.Failure("La convocatoria solo puede incluir jugadores de este equipo."), null);
        var match = new TeamMatch(teamId, userId, input.Opponent, input.StartsAt, input.Venue, input.Round);
        db.TeamMatches.Add(match);
        foreach (var playerId in playerIds) db.TeamCallups.Add(new TeamCallup(match.Id, playerId, userId));
        await db.SaveChangesAsync(cancellationToken);
        return (TeamOperationResult.Success(), match.Id);
    }

    public async Task<TeamOperationResult> SetAttendanceAsync(Guid userId, Guid teamId, Guid matchId, TeamAttendanceInput input, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return TeamOperationResult.Failure("No se ha encontrado la cuenta.");
        var callup = await db.TeamCallups.Join(db.TeamMatches, item => item.MatchId, match => match.Id, (item, match) => new { Callup = item, match.TeamId })
            .Where(item => item.TeamId == teamId && item.Callup.MatchId == matchId && item.Callup.PlayerId == userId)
            .Select(item => item.Callup).SingleOrDefaultAsync(cancellationToken);
        if (callup is null) return TeamOperationResult.Failure("No tienes una convocatoria para este partido.");
        try { callup.Respond(input.Attendance); }
        catch (ArgumentException) { return TeamOperationResult.Failure("Selecciona Confirmada o No disponible."); }
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> CreateEventAsync(Guid userId, Guid teamId, TeamEventInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        try { db.TeamEvents.Add(new TeamEvent(teamId, userId, input.Title, input.Kind, input.StartsAt, input.EndsAt, input.Venue, input.Notes)); }
        catch (ArgumentException exception) { return TeamOperationResult.Failure(exception.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> UpdateEventAsync(Guid userId, Guid teamId, Guid eventId, TeamEventInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        var teamEvent = await db.TeamEvents.SingleOrDefaultAsync(item => item.Id == eventId && item.TeamId == teamId && !item.IsCancelled, cancellationToken);
        if (teamEvent is null) return TeamOperationResult.Failure("No se ha encontrado el evento.");
        try { teamEvent.Update(input.Title, input.Kind, input.StartsAt, input.EndsAt, input.Venue, input.Notes); }
        catch (ArgumentException exception) { return TeamOperationResult.Failure(exception.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> DeleteEventAsync(Guid userId, Guid teamId, Guid eventId, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        var teamEvent = await db.TeamEvents.SingleOrDefaultAsync(item => item.Id == eventId && item.TeamId == teamId && !item.IsCancelled, cancellationToken);
        if (teamEvent is null) return TeamOperationResult.Failure("No se ha encontrado el evento.");
        teamEvent.Cancel();
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> SetTrainingPlanAsync(Guid userId, Guid teamId, TeamTrainingPlanInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        if (input.Slots.Count > 7 || input.Slots.Any(slot => !Enum.IsDefined(slot.DayOfWeek) || slot.EndsAt <= slot.StartsAt
                || slot.Venue?.Trim().Length > 200))
            return TeamOperationResult.Failure("Revisa los días, horarios y lugares de la planificación.");
        if (input.Slots.GroupBy(slot => (slot.DayOfWeek, slot.StartsAt)).Any(group => group.Count() > 1))
            return TeamOperationResult.Failure("No puede haber dos sesiones que empiecen el mismo día y a la misma hora.");
        var today = ClubToday();
        var seasonStartYear = today.Month >= 7 ? today.Year : today.Year - 1;
        var seasonStart = new DateOnly(seasonStartYear, 7, 1);
        var seasonEnd = new DateOnly(seasonStartYear + 1, 6, 30);
        if (input.EffectiveFrom < seasonStart || input.EffectiveFrom > seasonEnd)
            return TeamOperationResult.Failure("La fecha efectiva debe estar dentro de la temporada actual.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var sameDatePlan = await db.TeamWeeklyTrainingPlans.Include(plan => plan.Slots)
            .SingleOrDefaultAsync(plan => plan.TeamId == teamId && plan.EffectiveFrom == input.EffectiveFrom, cancellationToken);
        if (sameDatePlan is not null)
        {
            db.TeamWeeklyTrainingPlans.Remove(sameDatePlan);
            await db.SaveChangesAsync(cancellationToken);
        }
        var plan = new TeamWeeklyTrainingPlan(teamId, userId, input.EffectiveFrom);
        plan.SetEmpty(input.Slots.Count == 0);
        foreach (var slot in input.Slots)
            plan.Slots.Add(new TeamWeeklyTrainingSlot(plan.Id, slot.DayOfWeek, slot.StartsAt, slot.EndsAt, slot.Venue));
        db.TeamWeeklyTrainingPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> SetTrainingExceptionAsync(Guid userId, Guid teamId, Guid slotId, DateOnly date, TeamTrainingExceptionInput input, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        var today = ClubToday();
        var seasonStartYear = today.Month >= 7 ? today.Year : today.Year - 1;
        if (date < new DateOnly(seasonStartYear, 7, 1) || date > new DateOnly(seasonStartYear + 1, 6, 30))
            return TeamOperationResult.Failure("La fecha debe pertenecer a la temporada actual.");
        var slot = await db.TeamWeeklyTrainingSlots.Include(item => item.Plan)
            .SingleOrDefaultAsync(item => item.Id == slotId && item.Plan.TeamId == teamId && item.DayOfWeek == date.DayOfWeek
                && item.Plan.EffectiveFrom <= date, cancellationToken);
        if (slot is null) return TeamOperationResult.Failure("No se ha encontrado la sesión planificada para esa fecha.");
        var nextPlanDate = await db.TeamWeeklyTrainingPlans.Where(plan => plan.TeamId == teamId && plan.EffectiveFrom > slot.Plan.EffectiveFrom)
            .Select(plan => (DateOnly?)plan.EffectiveFrom).MinAsync(cancellationToken);
        if (nextPlanDate is not null && date >= nextPlanDate.Value)
            return TeamOperationResult.Failure("La planificación de esa fecha ha cambiado; recarga el calendario.");
        try
        {
            var existing = await db.TeamTrainingExceptions.SingleOrDefaultAsync(item => item.SlotId == slotId && item.Date == date, cancellationToken);
            if (existing is not null) db.TeamTrainingExceptions.Remove(existing);
            db.TeamTrainingExceptions.Add(new TeamTrainingException(slotId, date, input.IsCancelled, input.StartsAt, input.EndsAt, input.Venue));
        }
        catch (ArgumentException exception) { return TeamOperationResult.Failure(exception.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> DeleteTrainingExceptionAsync(Guid userId, Guid teamId, Guid slotId, DateOnly date, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para gestionar este equipo.");
        var exception = await db.TeamTrainingExceptions.SingleOrDefaultAsync(item => item.SlotId == slotId && item.Date == date
            && db.TeamWeeklyTrainingSlots.Any(slot => slot.Id == item.SlotId && slot.Plan.TeamId == teamId), cancellationToken);
        if (exception is null) return TeamOperationResult.Failure("No se ha encontrado la excepción de esa sesión.");
        db.TeamTrainingExceptions.Remove(exception);
        await db.SaveChangesAsync(cancellationToken);
        return TeamOperationResult.Success();
    }

    public async Task<TeamOperationResult> CreateBoardPostAsync(Guid userId, Guid teamId, TeamBoardInput input, IReadOnlyList<TeamBoardFile> files, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(userId, teamId, cancellationToken)) return TeamOperationResult.Failure("No tienes permisos para publicar en la pizarra de este equipo.");
        var content = input.Content?.Trim() ?? string.Empty;
        if (content.Length > 4000 || (string.IsNullOrWhiteSpace(content) && files.Count == 0)) return TeamOperationResult.Failure("Escribe un mensaje (máximo 4000 caracteres) o adjunta un archivo.");
        if (files.Count > MaxFiles) return TeamOperationResult.Failure("Se permiten hasta cuatro archivos por mensaje.");
        if (files.Sum(file => file.Length) > MaxTotalFileBytes) return TeamOperationResult.Failure("El tamaño total de los adjuntos no puede superar 400 MB.");
        var post = new TeamBoardPost(teamId, userId, string.IsNullOrWhiteSpace(content) ? "Adjunto del equipo" : content);
        var keys = new List<string>();
        try
        {
            foreach (var file in files)
            {
                if (!AllowedFiles.TryGetValue(file.ContentType, out var type) || file.Length <= 0 || file.Length > MaxFileBytes)
                    throw new ArgumentException("Admite imágenes, vídeos o PDF de hasta 100 MB por archivo.");
                if (!await HasValidSignatureAsync(file.Content, file.ContentType, cancellationToken)) throw new ArgumentException("El contenido no coincide con el tipo de archivo indicado.");
                file.Content.Position = 0;
                var key = await storage.SaveAsync(Guid.NewGuid(), type.Extension, file.Content, cancellationToken);
                keys.Add(key);
                db.TeamBoardAttachments.Add(new TeamBoardAttachment(post.Id, key, file.FileName, file.ContentType, file.Length));
            }
            db.TeamBoardPosts.Add(post);
            await db.SaveChangesAsync(cancellationToken);
            return TeamOperationResult.Success();
        }
        catch (ArgumentException exception)
        {
            foreach (var key in keys) await storage.DeleteAsync(key, CancellationToken.None);
            return TeamOperationResult.Failure(exception.Message);
        }
        catch
        {
            foreach (var key in keys) await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }
    }

    public async Task<(Stream Content, string ContentType, string FileName)?> OpenBoardAttachmentAsync(Guid userId, Guid teamId, Guid attachmentId, CancellationToken cancellationToken)
    {
        if (await GetAccessAsync(userId, teamId, cancellationToken) is null) return null;
        var attachment = await db.TeamBoardAttachments.AsNoTracking()
            .Join(db.TeamBoardPosts.AsNoTracking().Where(post => post.TeamId == teamId), item => item.BoardPostId, post => post.Id, (item, _) => item)
            .SingleOrDefaultAsync(item => item.Id == attachmentId, cancellationToken);
        if (attachment is null) return null;
        var content = await storage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return content is null ? null : (content, attachment.ContentType, attachment.FileName);
    }

    private async Task<string?> GetAccessAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive) return null;
        if (await users.IsInRoleAsync(user, "Administrador")) return "Administrador";
        if (await users.IsInRoleAsync(user, "CuerpoTecnico") && await db.TeamAssignments.AnyAsync(item => item.UserId == userId && item.TeamId == teamId, cancellationToken)) return "CuerpoTecnico";
        if ((await users.IsInRoleAsync(user, "Jugador") || await users.IsInRoleAsync(user, "CuerpoTecnico"))
            && await db.TeamPlayerMemberships.AnyAsync(item => item.UserId == userId && item.TeamId == teamId, cancellationToken)) return "Jugador";
        return null;
    }

    private async Task<bool> CanManageAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(userId, teamId, cancellationToken);
        return access is "Administrador" or "CuerpoTecnico";
    }

    private async Task<bool> IsAdministratorAsync(Guid userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user is not null && await users.IsInRoleAsync(user, "Administrador");
    }

    private static async Task<bool> HasValidSignatureAsync(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek) return false;
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        stream.Position = 0;
        return contentType switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            "image/png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            "image/webp" => read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "video/mp4" => read >= 8 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            "video/webm" => read >= 4 && header.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1a, 0x45, 0xdf, 0xa3 }),
            "application/pdf" => read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            _ => false
        };
    }

    private static readonly IReadOnlyDictionary<string, (string Extension, string Kind)> AllowedFiles = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = (".jpg", "image"), ["image/png"] = (".png", "image"), ["image/webp"] = (".webp", "image"),
        ["video/mp4"] = (".mp4", "video"), ["video/webm"] = (".webm", "video"), ["application/pdf"] = (".pdf", "document")
    };

    private const int MaxFiles = 4;
    private const long MaxFileBytes = 100L * 1024 * 1024;
    private const long MaxTotalFileBytes = 400L * 1024 * 1024;
}
