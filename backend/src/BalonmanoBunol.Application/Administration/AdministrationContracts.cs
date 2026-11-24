namespace BalonmanoBunol.Application.Administration;

public sealed record AdminUserDto(Guid Id, string DisplayName, string Email, IReadOnlyList<string> Roles, IReadOnlyList<AssignedTeamDto> Teams);
public sealed record AssignedTeamDto(Guid Id, string Name, string Category);
public sealed record AdminTeamDto(Guid Id, string Name, string Category, bool IsActive);
public sealed record TeamInput(string Name, string Category);
public sealed record AssignTeamsInput(IReadOnlyList<Guid> TeamIds);
public sealed record ChangeRoleInput(string Role);
public sealed record AdminPlayerAssignmentInput(Guid TeamId, int? JerseyNumber, string Position);
public sealed record AdminTeamPlayerDto(Guid UserId, string DisplayName, string Email, int? JerseyNumber, string Position);
public sealed record AdminOperationResult(bool Succeeded, string? Error)
{
    public static AdminOperationResult Success() => new(true, null);
    public static AdminOperationResult Failure(string error) => new(false, error);
}
