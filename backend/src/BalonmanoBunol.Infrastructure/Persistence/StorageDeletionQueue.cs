using BalonmanoBunol.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class StorageDeletionQueue(ClubDbContext db, IServiceScopeFactory scopeFactory) : IStorageDeletionQueue
{
    public Task EnqueueAsync(string storageKind, string storageKey, CancellationToken cancellationToken) => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO storage_deletion_jobs ("Id", storage_kind, storage_key, attempts, available_at)
            VALUES ({Guid.NewGuid()}, {storageKind}, {storageKey}, 0, {DateTimeOffset.UtcNow})
            ON CONFLICT (storage_kind, storage_key) DO NOTHING
            """, cancellationToken);

    public async Task EnqueueDurableAsync(string storageKind, string storageKey, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var independentDb = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        await independentDb.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO storage_deletion_jobs ("Id", storage_kind, storage_key, attempts, available_at)
            VALUES ({Guid.NewGuid()}, {storageKind}, {storageKey}, 0, {DateTimeOffset.UtcNow})
            ON CONFLICT (storage_kind, storage_key) DO NOTHING
            """, cancellationToken);
    }
}
