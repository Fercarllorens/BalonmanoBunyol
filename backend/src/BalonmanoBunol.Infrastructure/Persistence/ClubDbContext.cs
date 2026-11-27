using BalonmanoBunol.Domain.Social;
using BalonmanoBunol.Domain.Teams;
using BalonmanoBunol.Domain.Administration;
using BalonmanoBunol.Domain.Auth;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Social;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class ClubDbContext(DbContextOptions<ClubDbContext> options)
    : IdentityDbContext<ClubUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamAssignment> TeamAssignments => Set<TeamAssignment>();
    public DbSet<TeamPlayerMembership> TeamPlayerMemberships => Set<TeamPlayerMembership>();
    public DbSet<TeamMatch> TeamMatches => Set<TeamMatch>();
    public DbSet<TeamCallup> TeamCallups => Set<TeamCallup>();
    public DbSet<TeamEvent> TeamEvents => Set<TeamEvent>();
    public DbSet<TeamWeeklyTrainingPlan> TeamWeeklyTrainingPlans => Set<TeamWeeklyTrainingPlan>();
    public DbSet<TeamWeeklyTrainingSlot> TeamWeeklyTrainingSlots => Set<TeamWeeklyTrainingSlot>();
    public DbSet<TeamTrainingException> TeamTrainingExceptions => Set<TeamTrainingException>();
    public DbSet<TeamBoardPost> TeamBoardPosts => Set<TeamBoardPost>();
    public DbSet<TeamBoardAttachment> TeamBoardAttachments => Set<TeamBoardAttachment>();
    public DbSet<AdministrationAuditEntry> AdministrationAuditEntries => Set<AdministrationAuditEntry>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<RoleRequest> RoleRequests => Set<RoleRequest>();
    public DbSet<PostAttachment> PostAttachments => Set<PostAttachment>();
    public DbSet<DailyUploadUsage> DailyUploadUsages => Set<DailyUploadUsage>();
    public DbSet<StorageDeletionJob> StorageDeletionJobs => Set<StorageDeletionJob>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Team>(entity =>
        {
            entity.ToTable("teams");
            entity.HasKey(team => team.Id);
            entity.Property(team => team.Name).HasMaxLength(120).IsRequired();
            entity.Property(team => team.Category).HasMaxLength(80).IsRequired();
            entity.HasIndex(team => new { team.Category, team.Name });
        });

        builder.Entity<TeamAssignment>(entity =>
        {
            entity.ToTable("team_assignments");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.TeamId }).IsUnique();
            entity.HasIndex(item => item.TeamId);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Team>().WithMany().HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeamPlayerMembership>(entity =>
        {
            entity.ToTable("team_player_memberships");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Position).HasMaxLength(80).IsRequired();
            entity.HasIndex(item => new { item.TeamId, item.UserId }).IsUnique();
            entity.HasOne<Team>().WithMany().HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeamMatch>(entity =>
        {
            entity.ToTable("team_matches");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Opponent).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Venue).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Round).HasMaxLength(80).IsRequired();
            entity.HasIndex(item => new { item.TeamId, item.StartsAt });
            entity.HasOne<Team>().WithMany().HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeamCallup>(entity =>
        {
            entity.ToTable("team_callups");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Attendance).HasMaxLength(30).IsRequired();
            entity.HasIndex(item => new { item.MatchId, item.PlayerId }).IsUnique();
            entity.HasOne<TeamMatch>().WithMany().HasForeignKey(item => item.MatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.PlayerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.CalledUpByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeamEvent>(entity =>
        {
            entity.ToTable("team_events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Kind).HasMaxLength(30).IsRequired();
            entity.Property(item => item.Venue).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(1000).IsRequired();
            entity.HasIndex(item => new { item.TeamId, item.StartsAt });
            entity.HasIndex(item => new { item.TeamId, item.IsCancelled, item.StartsAt });
            entity.HasOne<Team>().WithMany().HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeamWeeklyTrainingPlan>(entity =>
        {
            entity.ToTable("team_weekly_training_plans");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.TeamId, item.EffectiveFrom }).IsUnique();
            entity.Property(item => item.IsEmpty).IsRequired();
            entity.HasOne(item => item.Team).WithMany().HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(item => item.Slots).WithOne(slot => slot.Plan).HasForeignKey(slot => slot.PlanId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TeamWeeklyTrainingSlot>(entity =>
        {
            entity.ToTable("team_weekly_training_slots");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Venue).HasMaxLength(200).IsRequired();
            entity.HasIndex(item => new { item.PlanId, item.DayOfWeek, item.StartsAt }).IsUnique();
        });

        builder.Entity<TeamTrainingException>(entity =>
        {
            entity.ToTable("team_training_exceptions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Venue).HasMaxLength(200).IsRequired();
            entity.HasIndex(item => new { item.SlotId, item.Date }).IsUnique();
            entity.HasOne(item => item.Slot).WithMany().HasForeignKey(item => item.SlotId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TeamBoardPost>(entity =>
        {
            entity.ToTable("team_board_posts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Content).HasMaxLength(4000).IsRequired();
            entity.HasIndex(item => new { item.TeamId, item.CreatedAt });
            entity.HasOne<Team>().WithMany().HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.AuthorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany<TeamBoardAttachment>().WithOne().HasForeignKey(item => item.BoardPostId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TeamBoardAttachment>(entity =>
        {
            entity.ToTable("team_board_attachments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(120).IsRequired();
            entity.HasIndex(item => item.BoardPostId);
        });

        builder.Entity<AdministrationAuditEntry>(entity =>
        {
            entity.ToTable("administration_audit_entries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Action).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Details).HasMaxLength(2000).IsRequired();
            entity.HasIndex(item => item.CreatedAt);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.AdministratorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.TargetUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Post>(entity =>
        {
            entity.ToTable("posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Content).HasMaxLength(5000).IsRequired();
            entity.Property(post => post.IsPublished).IsRequired();
            entity.HasIndex(post => new { post.CreatedAt, post.Id });
            entity.HasIndex(post => new { post.TeamId, post.CreatedAt });
            entity.HasIndex(post => new { post.IsPublished, post.CreatedAt });
            entity.HasIndex(post => new { post.AuthorId, post.IsPublished, post.CreatedAt });
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(post => post.AuthorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(post => post.Attachments).WithOne().HasForeignKey(attachment => attachment.PostId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PostAttachment>(entity =>
        {
            entity.ToTable("post_attachments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
            entity.HasIndex(item => item.PostId);
        });

        builder.Entity<RoleRequest>(entity =>
        {
            entity.ToTable("role_requests");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.RequestedRole).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(item => new { item.Status, item.CreatedAt });
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClubUser>().WithMany().HasForeignKey(item => item.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClubUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.FirstName).HasMaxLength(80).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(79).HasDefaultValue(string.Empty).IsRequired();
            entity.HasOne<Team>().WithMany().HasForeignKey(user => user.PreferredTeamId).OnDelete(DeleteBehavior.SetNull);
            entity.Property(user => user.AvatarStorageKey).HasMaxLength(80);
            entity.HasIndex(user => user.IsActive);
        });

        builder.Entity<DailyUploadUsage>(entity =>
        {
            entity.ToTable("upload_daily_usage");
            entity.HasKey(item => item.UsageDate);
            entity.Property(item => item.UsageDate).HasColumnName("usage_date");
            entity.Property(item => item.UsedBytes).HasColumnName("used_bytes").IsRequired();
        });

        builder.Entity<StorageDeletionJob>(entity =>
        {
            entity.ToTable("storage_deletion_jobs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StorageKind).HasColumnName("storage_kind").HasMaxLength(30).IsRequired();
            entity.Property(item => item.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();
            entity.Property(item => item.Attempts).HasColumnName("attempts").IsRequired();
            entity.Property(item => item.AvailableAt).HasColumnName("available_at").IsRequired();
            entity.Property(item => item.LastError).HasColumnName("last_error").HasMaxLength(500);
            entity.HasIndex(item => item.AvailableAt).HasDatabaseName("ix_storage_deletion_jobs_available_at");
            entity.HasIndex(item => new { item.StorageKind, item.StorageKey }).IsUnique().HasDatabaseName("ix_storage_deletion_jobs_kind_key");
        });

    }
}
