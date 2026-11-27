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
    private static readonly TimeSpan DeleteAttemptTimeout = TimeSpan.FromSeconds(30);

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

    private async Task<bool> ProcessOneAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
        var job = await db.StorageDeletionJobs.FromSqlInterpolated($"""
                SELECT "Id", storage_kind, storage_key, attempts, available_at, last_error FROM storage_deletion_jobs
                WHERE available_at <= {DateTimeOffset.UtcNow}
                ORDER BY available_at
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .AsNoTracking().FirstOrDefaultAsync(stoppingToken);
        if (job is null)
        {
            await transaction.CommitAsync(stoppingToken);
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(DeleteAttemptTimeout);
        try
        {
            switch (job.StorageKind)
            {
                case "feed": await scope.ServiceProvider.GetRequiredService<IPostMediaStorage>().DeleteAsync(job.StorageKey, timeout.Token); break;
                case "avatar": await scope.ServiceProvider.GetRequiredService<IProfileAvatarStorage>().DeleteAsync(job.StorageKey, timeout.Token); break;
                case "team-board": await scope.ServiceProvider.GetRequiredService<ITeamBoardStorage>().DeleteAsync(job.StorageKey, timeout.Token); break;
                default: throw new InvalidOperationException("Tipo de almacenamiento desconocido en la cola.");
            }

            // Una cancelación ocurrida tras el borrado R2 debe dejar el trabajo para repetirlo de forma idempotente.
            stoppingToken.ThrowIfCancellationRequested();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM storage_deletion_jobs WHERE \"Id\" = {job.Id}", stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await RescheduleAsync(db, job, "TimeoutException", stoppingToken);
            logger.LogWarning("Se agotó el tiempo de eliminación de un objeto; se reintentará el trabajo.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var attempts = await RescheduleAsync(db, job, exception.GetType().Name, stoppingToken);
            logger.LogWarning("No se pudo eliminar objeto de almacenamiento ({ErrorType}); intento {Attempt}.", exception.GetType().Name, attempts);
        }

        stoppingToken.ThrowIfCancellationRequested();
        await transaction.CommitAsync(stoppingToken);
        return true;
    }

    private static async Task<int> RescheduleAsync(ClubDbContext db, StorageDeletionJob job, string errorType, CancellationToken cancellationToken)
    {
        var attempts = job.Attempts + 1;
        var availableAt = DateTimeOffset.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(attempts, 6))));
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE storage_deletion_jobs
            SET attempts = {attempts}, last_error = {errorType}, available_at = {availableAt}
            WHERE "Id" = {job.Id}
            """, cancellationToken);
        return attempts;
    }
}
