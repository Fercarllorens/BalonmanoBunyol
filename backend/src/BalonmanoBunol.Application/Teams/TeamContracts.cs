namespace BalonmanoBunol.Application.Teams;

public sealed record TeamSummaryDto(Guid Id, string Name, string Category, string Access);
public sealed record TeamPlayerDto(Guid UserId, string DisplayName, string Email, string? AvatarUrl, int? JerseyNumber, string Position, int MatchesPlayed, int MinutesPlayed, int Goals, int Saves);
public sealed record TeamMatchDto(Guid Id, string Opponent, DateTimeOffset StartsAt, string Venue, string Round, IReadOnlyList<TeamCallupDto> Callups);
public sealed record TeamCallupDto(Guid PlayerId, string DisplayName, int? JerseyNumber, string Position, string Attendance);
public sealed record TeamEventDto(Guid Id, string Title, string Kind, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, string Venue, string Notes, bool CanManage = false);
public sealed record TeamTrainingSlotDto(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartsAt, TimeOnly EndsAt, string Venue);
public sealed record TeamCalendarItemDto(string Id, string Source, string Kind, string Title, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, string Venue, string Notes, bool IsCancelled = false, Guid? TrainingSlotId = null, DateOnly? TrainingDate = null, bool CanManage = false, IReadOnlyList<TeamCallupDto>? Callups = null);
public sealed record TeamTrainingPlanDto(DateOnly EffectiveFrom, IReadOnlyList<TeamTrainingSlotDto> Slots);
public sealed record TeamCalendarDto(DateOnly SeasonStart, DateOnly SeasonEnd, DateOnly Month, IReadOnlyList<TeamCalendarItemDto> Items, IReadOnlyList<TeamTrainingPlanDto> Plans);
public sealed record TeamBoardAttachmentDto(Guid Id, string FileName, string ContentType, long Length, string Url);
public sealed record TeamBoardPostDto(Guid Id, Guid AuthorId, string AuthorName, string Content, DateTimeOffset CreatedAt, IReadOnlyList<TeamBoardAttachmentDto> Attachments);
public sealed record TeamSpaceDto(TeamSummaryDto Team, IReadOnlyList<TeamPlayerDto> Players, IReadOnlyList<TeamMatchDto> Matches, IReadOnlyList<TeamEventDto> Events, IReadOnlyList<TeamBoardPostDto> BoardPosts, bool CanManage);
public sealed record TeamPlayerSearchDto(Guid UserId, string DisplayName, string Email);
public sealed record TeamPlayerInput(Guid UserId, int? JerseyNumber, string Position);
public sealed record TeamPlayerStatsInput(Guid UserId, int MatchesPlayed, int MinutesPlayed, int Goals, int Saves);
public sealed record TeamMatchInput(string Opponent, DateTimeOffset StartsAt, string Venue, string? Round, IReadOnlyList<Guid> CalledUpPlayerIds);
public sealed record TeamAttendanceInput(string Attendance);
public sealed record TeamEventInput(string Title, string Kind, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, string Venue, string? Notes);
public sealed record TeamTrainingSlotInput(DayOfWeek DayOfWeek, TimeOnly StartsAt, TimeOnly EndsAt, string? Venue);
public sealed record TeamTrainingPlanInput(DateOnly EffectiveFrom, IReadOnlyList<TeamTrainingSlotInput> Slots);
public sealed record TeamTrainingExceptionInput(bool IsCancelled, TimeOnly? StartsAt, TimeOnly? EndsAt, string? Venue);
public sealed record TeamBoardInput(string Content);
public sealed record TeamBoardFile(string FileName, string ContentType, long Length, Stream Content);
public sealed record TeamOperationResult(bool Succeeded, string? Error)
{
    public static TeamOperationResult Success() => new(true, null);
    public static TeamOperationResult Failure(string error) => new(false, error);
}
