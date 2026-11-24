namespace BalonmanoBunol.Application.Teams;

public interface ITeamSpaceService
{
    Task<IReadOnlyList<TeamSummaryDto>> GetMyTeamsAsync(Guid userId, CancellationToken cancellationToken);
    Task<TeamSpaceDto?> GetTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken);
    Task<TeamCalendarDto?> GetCalendarAsync(Guid userId, Guid teamId, int year, int month, CancellationToken cancellationToken);
    Task<IReadOnlyList<TeamPlayerSearchDto>> SearchPlayersAsync(Guid userId, Guid teamId, string query, CancellationToken cancellationToken);
    Task<TeamOperationResult> AddPlayerAsync(Guid userId, Guid teamId, TeamPlayerInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> RemovePlayerAsync(Guid userId, Guid teamId, Guid playerId, CancellationToken cancellationToken);
    Task<TeamOperationResult> UpdatePlayerAsync(Guid userId, Guid teamId, TeamPlayerInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> UpdatePlayerStatsAsync(Guid userId, Guid teamId, TeamPlayerStatsInput input, CancellationToken cancellationToken);
    Task<(TeamOperationResult Result, Guid? MatchId)> CreateMatchAsync(Guid userId, Guid teamId, TeamMatchInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> SetAttendanceAsync(Guid userId, Guid teamId, Guid matchId, TeamAttendanceInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> CreateEventAsync(Guid userId, Guid teamId, TeamEventInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> UpdateEventAsync(Guid userId, Guid teamId, Guid eventId, TeamEventInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> DeleteEventAsync(Guid userId, Guid teamId, Guid eventId, CancellationToken cancellationToken);
    Task<TeamOperationResult> SetTrainingPlanAsync(Guid userId, Guid teamId, TeamTrainingPlanInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> SetTrainingExceptionAsync(Guid userId, Guid teamId, Guid slotId, DateOnly date, TeamTrainingExceptionInput input, CancellationToken cancellationToken);
    Task<TeamOperationResult> DeleteTrainingExceptionAsync(Guid userId, Guid teamId, Guid slotId, DateOnly date, CancellationToken cancellationToken);
    Task<TeamOperationResult> CreateBoardPostAsync(Guid userId, Guid teamId, TeamBoardInput input, IReadOnlyList<TeamBoardFile> files, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType, string FileName)?> OpenBoardAttachmentAsync(Guid userId, Guid teamId, Guid attachmentId, CancellationToken cancellationToken);
}
