namespace BalonmanoBunol.Application.Profile;

public sealed record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    string Email,
    Guid? PreferredTeamId,
    string? AvatarUrl,
    IReadOnlyList<TeamPreferenceOptionDto> AvailableTeams);

public sealed record TeamPreferenceOptionDto(Guid Id, string Name, string Category);

public sealed record UpdateProfileRequest(string FirstName, string LastName, string Email, Guid? PreferredTeamId);

public sealed record ProfileOperationResult(bool Succeeded, UserProfileDto? Profile, string? Error)
{
    public static ProfileOperationResult Success(UserProfileDto profile) => new(true, profile, null);
    public static ProfileOperationResult Failure(string error) => new(false, null, error);
}
