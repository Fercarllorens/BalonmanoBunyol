namespace BalonmanoBunol.Application.Teams;

public interface ITeamBoardStorage
{
    Task<string> SaveAsync(Guid id, string extension, Stream content, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
