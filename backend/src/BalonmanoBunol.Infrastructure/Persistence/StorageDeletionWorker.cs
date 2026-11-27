using BalonmanoBunol.Application.Profile;
using BalonmanoBunol.Application.Social;
using BalonmanoBunol.Application.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class StorageDeletionWorker(IServiceScopeFactory scopeFactory, ILogger<StorageDeletionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await ProcessOneAsync(stoppingToken)) await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError("Error procesando la cola de eliminación de archivos ({ErrorType}).", exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var job = await db.StorageDeletionJobs.FromSqlInterpolated($"""
                SELECT "Id", storage_kind, storage_key, attempts, available_at, last_error FROM storage_deletion_jobs
                WHERE available_at <= {DateTimeOffset.UtcNow}
                ORDER BY available_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        try
        {
            switch (job.StorageKind)
            {
                case "feed": await scope.ServiceProvider.GetRequiredService<IPostMediaStorage>().DeleteAsync(job.StorageKey, CancellationToken.None); break;
                case "avatar": await scope.ServiceProvider.GetRequiredService<IProfileAvatarStorage>().DeleteAsync(job.StorageKey, CancellationToken.None); break;
                case "team-board": await scope.ServiceProvider.GetRequiredService<ITeamBoardStorage>().DeleteAsync(job.StorageKey, CancellationToken.None); break;
                default: throw new InvalidOperationException("Tipo de almacenamiento desconocido en la cola.");
            }
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM storage_deletion_jobs WHERE \"Id\" = {job.Id}", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var attempts = job.Attempts + 1;
            var availableAt = DateTimeOffset.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(attempts, 6))));
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE storage_deletion_jobs
                SET attempts = {attempts}, last_error = {exception.GetType().Name}, available_at = {availableAt}
                WHERE "Id" = {job.Id}
                """, cancellationToken);
            logger.LogWarning("No se pudo eliminar objeto de almacenamiento ({ErrorType}); intento {Attempt}.", exception.GetType().Name, attempts);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
