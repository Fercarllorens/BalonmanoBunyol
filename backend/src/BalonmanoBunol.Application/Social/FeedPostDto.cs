namespace BalonmanoBunol.Application.Social;

public sealed record FeedPostDto(
    Guid Id,
    Guid AuthorId,
    Guid? TeamId,
    string Content,
    DateTimeOffset CreatedAt);
