namespace BalonmanoBunol.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task LogoutAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleRequestDto>> GetRoleRequestsAsync(CancellationToken cancellationToken);
    Task<bool> ResolveRoleRequestAsync(Guid requestId, Guid administratorId, bool approve, CancellationToken cancellationToken);
}
