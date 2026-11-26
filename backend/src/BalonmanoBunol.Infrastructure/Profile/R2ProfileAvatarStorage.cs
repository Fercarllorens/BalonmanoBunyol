using BalonmanoBunol.Application.Profile;
using BalonmanoBunol.Infrastructure.Persistence;

namespace BalonmanoBunol.Infrastructure.Profile;

public sealed class R2ProfileAvatarStorage(R2ObjectStorage storage) : IProfileAvatarStorage
{
    public Task<string> SaveAsync(Guid avatarId, string extension, Stream content, CancellationToken cancellationToken) =>
        storage.SaveAsync(avatarId, extension, content, cancellationToken);

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        storage.DeleteAsync(storageKey, cancellationToken);

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
        storage.OpenReadAsync(storageKey, cancellationToken);
}
