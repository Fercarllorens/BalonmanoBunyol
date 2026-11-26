using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamWeeklyTrainingPlan : Entity
{
    private TeamWeeklyTrainingPlan() { }

    public TeamWeeklyTrainingPlan(Guid teamId, Guid createdByUserId, DateOnly effectiveFrom)
    {
        if (teamId == Guid.Empty || createdByUserId == Guid.Empty)
            throw new ArgumentException("El equipo y la persona responsable son obligatorios.");
        TeamId = teamId;
        CreatedByUserId = createdByUserId;
        EffectiveFrom = effectiveFrom;
    }

    public Guid TeamId { get; private set; }
    public Team Team { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public bool IsEmpty { get; private set; }
    public ICollection<TeamWeeklyTrainingSlot> Slots { get; private set; } = new List<TeamWeeklyTrainingSlot>();

    public void SetEmpty(bool isEmpty) => IsEmpty = isEmpty;
}
