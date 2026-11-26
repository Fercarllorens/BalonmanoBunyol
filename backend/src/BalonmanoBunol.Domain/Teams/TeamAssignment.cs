using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamAssignment : Entity
{
    private TeamAssignment() { }

    public TeamAssignment(Guid userId, Guid teamId, Guid assignedByUserId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El usuario es obligatorio.", nameof(userId));
        if (teamId == Guid.Empty) throw new ArgumentException("El equipo es obligatorio.", nameof(teamId));
        if (assignedByUserId == Guid.Empty) throw new ArgumentException("El administrador es obligatorio.", nameof(assignedByUserId));
        UserId = userId;
        TeamId = teamId;
        AssignedByUserId = assignedByUserId;
    }

    public Guid UserId { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
}
