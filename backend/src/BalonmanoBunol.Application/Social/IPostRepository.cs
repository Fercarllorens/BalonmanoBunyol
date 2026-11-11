namespace BalonmanoBunol.Application.Social;

public interface IPostRepository
{
    Task<IReadOnlyList<FeedPostDto>> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, CancellationToken cancellationToken);
}
