using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamPlayerMembership : Entity
{
    private TeamPlayerMembership() { }

    public TeamPlayerMembership(Guid teamId, Guid userId, Guid addedByUserId, int? jerseyNumber, string position)
    {
        if (teamId == Guid.Empty || userId == Guid.Empty || addedByUserId == Guid.Empty)
            throw new ArgumentException("Equipo, jugador y responsable son obligatorios.");
        if (jerseyNumber is < 0 or > 99) throw new ArgumentOutOfRangeException(nameof(jerseyNumber));
        ArgumentException.ThrowIfNullOrWhiteSpace(position);
        TeamId = teamId;
        UserId = userId;
        AddedByUserId = addedByUserId;
        JerseyNumber = jerseyNumber;
        Position = position.Trim();
    }

    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AddedByUserId { get; private set; }
    public int? JerseyNumber { get; private set; }
    public string Position { get; private set; } = string.Empty;
    public int MatchesPlayed { get; private set; }
    public int MinutesPlayed { get; private set; }
    public int Goals { get; private set; }
    public int Saves { get; private set; }

    public void Update(int? jerseyNumber, string position, int matchesPlayed, int minutesPlayed, int goals, int saves)
    {
        if (jerseyNumber is < 0 or > 99 || matchesPlayed < 0 || minutesPlayed < 0 || goals < 0 || saves < 0)
            throw new ArgumentOutOfRangeException(nameof(matchesPlayed), "Las estadísticas y el dorsal no pueden ser negativos ni superar los límites permitidos.");
        ArgumentException.ThrowIfNullOrWhiteSpace(position);
        JerseyNumber = jerseyNumber;
        Position = position.Trim();
        MatchesPlayed = matchesPlayed;
        MinutesPlayed = minutesPlayed;
        Goals = goals;
        Saves = saves;
    }
}
