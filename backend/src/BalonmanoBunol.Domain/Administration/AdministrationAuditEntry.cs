using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Administration;

public sealed class AdministrationAuditEntry : Entity
{
    private AdministrationAuditEntry() { }

    public AdministrationAuditEntry(Guid administratorId, Guid? targetUserId, string action, string details)
    {
        if (administratorId == Guid.Empty) throw new ArgumentException("El administrador es obligatorio.", nameof(administratorId));
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);
        AdministratorId = administratorId;
        TargetUserId = targetUserId;
        Action = action.Trim();
        Details = details.Trim();
    }

    public Guid AdministratorId { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
}
