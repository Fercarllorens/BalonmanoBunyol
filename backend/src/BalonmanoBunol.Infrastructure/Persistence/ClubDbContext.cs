using BalonmanoBunol.Domain.Social;
using BalonmanoBunol.Domain.Teams;
using BalonmanoBunol.Domain.Auth;
using BalonmanoBunol.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class ClubDbContext(DbContextOptions<ClubDbContext> options)
    : IdentityDbContext<ClubUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<RoleRequest> RoleRequests => Set<RoleRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Team>(entity =>
        {
            entity.ToTable("teams");
            entity.HasKey(team => team.Id);
            entity.Property(team => team.Name).HasMaxLength(120).IsRequired();
            entity.Property(team => team.Category).HasMaxLength(80).IsRequired();
            entity.Property(team => team.Season).HasMaxLength(20).IsRequired();
            entity.HasIndex(team => new { team.Season, team.Category });
        });

        builder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Content).HasMaxLength(5000).IsRequired();
            entity.HasIndex(post => new { post.CreatedAt, post.Id });
            entity.HasIndex(post => new { post.TeamId, post.CreatedAt });
        });

        builder.Entity<ClubUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
            entity.HasIndex(user => user.IsActive);
        });

        builder.Entity<RoleRequest>(entity =>
        {
            entity.ToTable("role_requests");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.RequestedRole).HasMaxLength(40).IsRequired();
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(request => new { request.Status, request.CreatedAt });
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(request => request.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(request => request.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
