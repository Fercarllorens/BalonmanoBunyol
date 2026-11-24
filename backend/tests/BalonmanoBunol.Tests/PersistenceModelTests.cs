using BalonmanoBunol.Domain.Social;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BalonmanoBunol.Tests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void PostAttachmentRelationship_UsesPostIdWithoutShadowForeignKey()
    {
        var options = new DbContextOptionsBuilder<ClubDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=test;Password=test")
            .Options;
        using var context = new ClubDbContext(options);
        var attachment = context.Model.FindEntityType(typeof(PostAttachment));
        Assert.NotNull(attachment);

        var foreignKeys = attachment!.GetForeignKeys().ToArray();
        var foreignKey = Assert.Single(foreignKeys);
        Assert.Equal(typeof(Post), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(PostAttachment.PostId), Assert.Single(foreignKey.Properties).Name);
        Assert.DoesNotContain(attachment.GetProperties(), property => property.IsShadowProperty());
    }
}
