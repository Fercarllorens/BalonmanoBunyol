using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Social;

public sealed class Post : Entity
{
    private Post() { }

    public Post(Guid authorId, string content, Guid? teamId = null)
    {
        if (authorId == Guid.Empty) throw new ArgumentException("El autor es obligatorio.", nameof(authorId));
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.Length > 5000) throw new ArgumentOutOfRangeException(nameof(content), "La publicación no puede superar 5000 caracteres.");

        AuthorId = authorId;
        Content = content.Trim();
        TeamId = teamId;
    }

    public Guid AuthorId { get; private set; }
    public Guid? TeamId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; } = true;
}
