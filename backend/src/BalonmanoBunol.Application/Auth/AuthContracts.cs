namespace BalonmanoBunol.Application.Auth;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string AccountType);

public sealed record LoginRequest(string Email, string Password, bool RememberMe);

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string FirstName = "",
    string LastName = "",
    Guid? PreferredTeamId = null,
    string? AvatarUrl = null);

public sealed record RoleRequestDecisionDto(Guid AdministratorId, bool Approve);

public sealed record RoleRequestDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Email,
    string RequestedRole,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    Guid? ResolvedByUserId);

public sealed record AuthResult(bool Succeeded, AuthUserDto? User, string? Error)
{
    public static AuthResult Success(AuthUserDto user) => new(true, user, null);
    public static AuthResult Failure(string error) => new(false, null, error);
}
