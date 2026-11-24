using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class TeamEvent : Entity
{
    private TeamEvent() { }

    public TeamEvent(Guid teamId, Guid createdByUserId, string title, string kind, DateTimeOffset startsAt, DateTimeOffset? endsAt, string venue, string? notes)
    {
        if (teamId == Guid.Empty || createdByUserId == Guid.Empty) throw new ArgumentException("El equipo y la persona responsable son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (kind is not ("Entrenamiento" or "Partido" or "Evento")) throw new ArgumentException("El tipo de evento no es válido.", nameof(kind));
        if (endsAt < startsAt) throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio.", nameof(endsAt));
        if (venue?.Trim().Length > 200) throw new ArgumentException("El lugar no puede superar 200 caracteres.", nameof(venue));
        TeamId = teamId;
        CreatedByUserId = createdByUserId;
        Title = title.Trim();
        Kind = kind;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Venue = venue?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public Guid TeamId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Kind { get; private set; } = string.Empty;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
    public string Venue { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public bool IsCancelled { get; private set; }

    public void Update(string title, string kind, DateTimeOffset startsAt, DateTimeOffset? endsAt, string? venue, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (kind is not ("Entrenamiento" or "Partido" or "Evento")) throw new ArgumentException("El tipo de evento no es válido.", nameof(kind));
        if (endsAt < startsAt) throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio.", nameof(endsAt));
        if (venue?.Trim().Length > 200) throw new ArgumentException("El lugar no puede superar 200 caracteres.", nameof(venue));
        Title = title.Trim();
        Kind = kind;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Venue = venue?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void Cancel() => IsCancelled = true;
}
