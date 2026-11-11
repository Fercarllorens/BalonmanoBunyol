namespace BalonmanoBunol.Application.Social;

public interface IFeedService
{
    Task<IReadOnlyList<FeedPostDto>> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, CancellationToken cancellationToken);
}
