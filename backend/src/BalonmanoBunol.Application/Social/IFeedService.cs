namespace BalonmanoBunol.Application.Social;

public interface IFeedService
{
    Task<FeedPageDto> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, bool officialOnly, CancellationToken cancellationToken);
    Task<FeedPostDto> CreateAsync(CreatePostCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid postId, Guid requesterId, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType, string FileName)?> OpenPublicAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken);
}
