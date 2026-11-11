using Microsoft.AspNetCore.Identity;

namespace BalonmanoBunol.Infrastructure.Identity;

public sealed class ClubUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
