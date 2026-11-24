namespace BalonmanoBunol.Application.Profile;

public interface IProfileAvatarStorage
{
    Task<string> SaveAsync(Guid avatarId, string extension, Stream content, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
