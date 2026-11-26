namespace BalonmanoBunol.Application.Social;

public sealed record FeedPostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    string AuthorProfile,
    Guid? TeamId,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PostAttachmentDto> Attachments);

public sealed record PostAttachmentDto(Guid Id, string FileName, string ContentType, long Length, string Url);

public sealed record CreatePostAttachment(string FileName, string ContentType, long Length, Stream Content);

public sealed record CreatePostCommand(Guid AuthorId, string AuthorDisplayName, IReadOnlyList<string> AuthorRoles, string? Content, IReadOnlyList<CreatePostAttachment> Attachments, string? AuthorAvatarUrl = null);

public sealed record FeedPageDto(IReadOnlyList<FeedPostDto> Posts, DateTimeOffset? NextBefore, Guid? NextBeforeId, bool HasMore);
