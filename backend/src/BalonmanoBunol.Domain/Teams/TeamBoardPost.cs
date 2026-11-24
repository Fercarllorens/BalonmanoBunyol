using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamBoardPost : Entity
{
    private TeamBoardPost() { }

    public TeamBoardPost(Guid teamId, Guid authorId, string content)
    {
        if (teamId == Guid.Empty || authorId == Guid.Empty) throw new ArgumentException("El equipo y autor son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.Length > 4000) throw new ArgumentOutOfRangeException(nameof(content), "El mensaje no puede superar 4000 caracteres.");
        TeamId = teamId;
        AuthorId = authorId;
        Content = content.Trim();
    }

    public Guid TeamId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;
}
