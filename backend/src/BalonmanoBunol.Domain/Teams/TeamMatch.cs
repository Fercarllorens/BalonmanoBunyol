using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamMatch : Entity
{
    private TeamMatch() { }

    public TeamMatch(Guid teamId, Guid createdByUserId, string opponent, DateTimeOffset startsAt, string venue, string? round)
    {
        if (teamId == Guid.Empty || createdByUserId == Guid.Empty) throw new ArgumentException("El equipo y la persona responsable son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(opponent);
        ArgumentException.ThrowIfNullOrWhiteSpace(venue);
        TeamId = teamId;
        CreatedByUserId = createdByUserId;
        Opponent = opponent.Trim();
        StartsAt = startsAt;
        Venue = venue.Trim();
        Round = round?.Trim() ?? string.Empty;
    }

    public Guid TeamId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Opponent { get; private set; } = string.Empty;
    public DateTimeOffset StartsAt { get; private set; }
    public string Venue { get; private set; } = string.Empty;
    public string Round { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public void Update(string opponent, DateTimeOffset startsAt, string venue, string? round)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opponent);
        ArgumentException.ThrowIfNullOrWhiteSpace(venue);
        Opponent = opponent.Trim();
        StartsAt = startsAt;
        Venue = venue.Trim();
        Round = round?.Trim() ?? string.Empty;
    }

    public void SetActive(bool active) => IsActive = active;
}
