using BalonmanoBunol.Application.Social;
using BalonmanoBunol.Infrastructure.Persistence;

namespace BalonmanoBunol.Infrastructure.Social;

public sealed class R2PostMediaStorage(R2ObjectStorage storage) : IPostMediaStorage
{
    public Task<string> SaveAsync(Guid attachmentId, string extension, Stream content, CancellationToken cancellationToken) =>
        storage.SaveAsync(attachmentId, extension, content, cancellationToken);

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        storage.DeleteAsync(storageKey, cancellationToken);

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
        storage.OpenReadAsync(storageKey, cancellationToken);
}
