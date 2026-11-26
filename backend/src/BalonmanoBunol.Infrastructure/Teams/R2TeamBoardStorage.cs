using BalonmanoBunol.Application.Teams;
using BalonmanoBunol.Infrastructure.Persistence;

namespace BalonmanoBunol.Infrastructure.Teams;

public sealed class R2TeamBoardStorage(R2ObjectStorage storage) : ITeamBoardStorage
{
    public Task<string> SaveAsync(Guid id, string extension, Stream content, CancellationToken cancellationToken) =>
        storage.SaveAsync(id, extension, content, cancellationToken);

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        storage.DeleteAsync(storageKey, cancellationToken);

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
        storage.OpenReadAsync(storageKey, cancellationToken);
}
