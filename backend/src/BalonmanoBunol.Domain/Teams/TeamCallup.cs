using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamCallup : Entity
{
    private TeamCallup() { }

    public TeamCallup(Guid matchId, Guid playerId, Guid calledUpByUserId)
    {
        if (matchId == Guid.Empty || playerId == Guid.Empty || calledUpByUserId == Guid.Empty)
            throw new ArgumentException("El partido, jugador y responsable son obligatorios.");
        MatchId = matchId;
        PlayerId = playerId;
        CalledUpByUserId = calledUpByUserId;
    }

    public Guid MatchId { get; private set; }
    public Guid PlayerId { get; private set; }
    public Guid CalledUpByUserId { get; private set; }
    public string Attendance { get; private set; } = "Pendiente";
    public DateTimeOffset? RespondedAt { get; private set; }

    public void Respond(string attendance)
    {
        if (attendance is not ("Confirmada" or "No disponible")) throw new ArgumentException("La respuesta de asistencia no es válida.", nameof(attendance));
        Attendance = attendance;
        RespondedAt = DateTimeOffset.UtcNow;
    }
}
