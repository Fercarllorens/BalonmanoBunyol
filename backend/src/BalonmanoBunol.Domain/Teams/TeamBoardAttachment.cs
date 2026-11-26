using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamBoardAttachment : Entity
{
    private TeamBoardAttachment() { }

    public TeamBoardAttachment(Guid boardPostId, string storageKey, string fileName, string contentType, long length)
    {
        if (boardPostId == Guid.Empty) throw new ArgumentException("El mensaje es obligatorio.", nameof(boardPostId));
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        BoardPostId = boardPostId;
        StorageKey = storageKey;
        FileName = Path.GetFileName(fileName);
        ContentType = contentType;
        Length = length;
    }

    public Guid BoardPostId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Length { get; private set; }
}
