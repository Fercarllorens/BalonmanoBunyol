namespace BalonmanoBunol.Application.Storage;

public interface IStorageDeletionQueue
{
    Task EnqueueAsync(string storageKind, string storageKey, CancellationToken cancellationToken);
}
