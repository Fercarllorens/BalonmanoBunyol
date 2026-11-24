namespace BalonmanoBunol.Application.Profile;

public interface IProfileService
{
    Task<UserProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<Guid?> GetDefaultPreferredTeamIdAsync(CancellationToken cancellationToken);
    Task<ProfileOperationResult> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
    Task<ProfileOperationResult> SetAvatarAsync(Guid userId, string contentType, long length, Stream content, CancellationToken cancellationToken);
    Task<bool> RemoveAvatarAsync(Guid userId, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType)?> OpenAvatarAsync(Guid userId, CancellationToken cancellationToken);
}
