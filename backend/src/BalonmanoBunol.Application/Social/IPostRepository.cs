namespace BalonmanoBunol.Application.Social;

public interface IPostRepository
{
    Task<IReadOnlyList<FeedPostDto>> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, bool officialOnly, CancellationToken cancellationToken);
    Task AddAsync(BalonmanoBunol.Domain.Social.Post post, IReadOnlyList<BalonmanoBunol.Domain.Social.PostAttachment> attachments, CancellationToken cancellationToken);
    Task<BalonmanoBunol.Domain.Social.Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken);
    Task<(string StorageKey, string ContentType, string FileName)?> GetPublicAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
