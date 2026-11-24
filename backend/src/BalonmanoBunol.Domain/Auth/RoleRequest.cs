using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Auth;

public sealed class RoleRequest : Entity
{
    private RoleRequest() { }

    public RoleRequest(Guid userId, string requestedRole)
    {
        if (userId == Guid.Empty) throw new ArgumentException("El usuario es obligatorio.", nameof(userId));
        if (requestedRole is not ("Jugador" or "CuerpoTecnico"))
            throw new ArgumentException("El rol solicitado no es válido.", nameof(requestedRole));

        UserId = userId;
        RequestedRole = requestedRole;
        Status = RoleRequestStatus.Pending;
    }

    public Guid UserId { get; private set; }
    public string RequestedRole { get; private set; } = string.Empty;
    public RoleRequestStatus Status { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public void Approve(Guid administratorId) => Resolve(administratorId, RoleRequestStatus.Approved);
    public void Reject(Guid administratorId) => Resolve(administratorId, RoleRequestStatus.Rejected);

    private void Resolve(Guid administratorId, RoleRequestStatus status)
    {
        if (administratorId == Guid.Empty) throw new ArgumentException("El administrador es obligatorio.", nameof(administratorId));
        if (Status != RoleRequestStatus.Pending) throw new InvalidOperationException("La solicitud ya ha sido resuelta.");
        Status = status;
        ResolvedByUserId = administratorId;
        ResolvedAt = DateTimeOffset.UtcNow;
    }
}
