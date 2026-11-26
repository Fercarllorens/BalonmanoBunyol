namespace BalonmanoBunol.Application.Social;

public interface IPostMediaStorage
{
    Task<string> SaveAsync(Guid attachmentId, string extension, Stream content, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
