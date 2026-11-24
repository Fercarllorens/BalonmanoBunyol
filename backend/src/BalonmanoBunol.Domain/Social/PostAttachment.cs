using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Social;

public sealed class PostAttachment : Entity
{
    private PostAttachment() { }

    public PostAttachment(Guid postId, string storageKey, string fileName, string contentType, long length)
    {
        if (postId == Guid.Empty) throw new ArgumentException("La publicación es obligatoria.", nameof(postId));
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (contentType is not ("image/jpeg" or "image/png" or "image/webp" or "video/mp4" or "video/webm"))
            throw new ArgumentException("El formato del adjunto no está permitido.", nameof(contentType));
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

        PostId = postId;
        StorageKey = storageKey;
        FileName = Path.GetFileName(fileName);
        ContentType = contentType;
        Length = length;
    }

    public Guid PostId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Length { get; private set; }
}
