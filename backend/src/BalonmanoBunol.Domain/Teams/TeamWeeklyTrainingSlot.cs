using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamWeeklyTrainingSlot : Entity
{
    private TeamWeeklyTrainingSlot() { }

    public TeamWeeklyTrainingSlot(Guid planId, DayOfWeek dayOfWeek, TimeOnly startsAt, TimeOnly endsAt, string? venue)
    {
        if (planId == Guid.Empty) throw new ArgumentException("La planificación es obligatoria.", nameof(planId));
        if (!Enum.IsDefined(dayOfWeek)) throw new ArgumentOutOfRangeException(nameof(dayOfWeek));
        if (endsAt <= startsAt) throw new ArgumentException("La hora de fin debe ser posterior a la hora de inicio.");
        if (venue?.Trim().Length > 200) throw new ArgumentException("El lugar no puede superar 200 caracteres.", nameof(venue));
        PlanId = planId;
        DayOfWeek = dayOfWeek;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Venue = venue?.Trim() ?? string.Empty;
    }

    public Guid PlanId { get; private set; }
    public TeamWeeklyTrainingPlan Plan { get; private set; } = null!;
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartsAt { get; private set; }
    public TimeOnly EndsAt { get; private set; }
    public string Venue { get; private set; } = string.Empty;
}
