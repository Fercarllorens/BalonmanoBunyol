using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamTrainingException : Entity
{
    private TeamTrainingException() { }

    public TeamTrainingException(Guid slotId, DateOnly date, bool isCancelled, TimeOnly? startsAt, TimeOnly? endsAt, string? venue)
    {
        if (slotId == Guid.Empty) throw new ArgumentException("La sesión planificada es obligatoria.", nameof(slotId));
        if (!isCancelled && (startsAt is null || endsAt is null || endsAt <= startsAt))
            throw new ArgumentException("Indica un horario válido para la sesión modificada.");
        if (venue?.Trim().Length > 200) throw new ArgumentException("El lugar no puede superar 200 caracteres.", nameof(venue));
        SlotId = slotId;
        Date = date;
        IsCancelled = isCancelled;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Venue = venue?.Trim() ?? string.Empty;
    }

    public Guid SlotId { get; private set; }
    public TeamWeeklyTrainingSlot Slot { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public bool IsCancelled { get; private set; }
    public TimeOnly? StartsAt { get; private set; }
    public TimeOnly? EndsAt { get; private set; }
    public string Venue { get; private set; } = string.Empty;
}
