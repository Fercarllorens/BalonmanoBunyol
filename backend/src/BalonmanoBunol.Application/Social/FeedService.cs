namespace BalonmanoBunol.Application.Social;

public sealed class FeedService(IPostRepository posts) : IFeedService
{
    public Task<IReadOnlyList<FeedPostDto>> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, CancellationToken cancellationToken)
    {
        var boundedPageSize = Math.Clamp(pageSize, 1, 50);
        return posts.GetLatestAsync(boundedPageSize, before, beforeId, cancellationToken);
    }
}
