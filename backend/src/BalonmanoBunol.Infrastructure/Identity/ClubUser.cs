using Microsoft.AspNetCore.Identity;

namespace BalonmanoBunol.Infrastructure.Identity;

public sealed class ClubUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? PreferredTeamId { get; set; }
    public string? AvatarStorageKey { get; set; }
    public bool IsActive { get; set; } = true;
}
