namespace BalonmanoBunol.Application.Administration;

public interface IAdministrationService
{
    Task<IReadOnlyList<AdminUserDto>> SearchUsersAsync(string query, bool coachesOnly, bool playersOnly, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminTeamDto>> GetTeamsAsync(CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateTeamAsync(Guid administratorId, TeamInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> UpdateTeamAsync(Guid administratorId, Guid teamId, TeamInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> SetTeamActiveAsync(Guid administratorId, Guid teamId, bool isActive, CancellationToken cancellationToken);
    Task<AdminOperationResult> AssignTeamsAsync(Guid administratorId, Guid userId, AssignTeamsInput input, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminTeamPlayerDto>> GetTeamPlayersAsync(Guid teamId, CancellationToken cancellationToken);
    Task<AdminOperationResult> AssignPlayerAsync(Guid administratorId, Guid userId, AdminPlayerAssignmentInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> RemovePlayerAsync(Guid administratorId, Guid userId, Guid teamId, CancellationToken cancellationToken);
    Task<AdminOperationResult> AddPlayerByStaffAsync(Guid staffId, Guid teamId, Guid playerId, int? jerseyNumber, string position, CancellationToken cancellationToken);
    Task<AdminOperationResult> RemovePlayerByStaffAsync(Guid staffId, Guid teamId, Guid playerId, CancellationToken cancellationToken);
    Task<AdminOperationResult> ChangeRoleAsync(Guid administratorId, Guid userId, ChangeRoleInput input, CancellationToken cancellationToken);
}
