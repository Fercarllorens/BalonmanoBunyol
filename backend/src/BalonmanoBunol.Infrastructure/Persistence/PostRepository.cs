using BalonmanoBunol.Application.Social;
using Microsoft.EntityFrameworkCore;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class PostRepository(ClubDbContext dbContext) : IPostRepository
{
    public async Task<IReadOnlyList<FeedPostDto>> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, CancellationToken cancellationToken)
    {
        var query = dbContext.Posts.AsNoTracking().Where(post => post.IsPublished);
        if (before is not null && beforeId is not null)
        {
            query = query.Where(post => post.CreatedAt < before.Value || (post.CreatedAt == before.Value && post.Id.CompareTo(beforeId.Value) < 0));
        }
        else if (before is not null)
        {
            query = query.Where(post => post.CreatedAt < before.Value);
        }

        return await query
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Take(pageSize)
            .Select(post => new FeedPostDto(post.Id, post.AuthorId, post.TeamId, post.Content, post.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
