using BalonmanoBunol.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class PostgresDailyUploadQuota(ClubDbContext db, IConfiguration configuration) : IUploadQuota
{
    private const long DefaultLimitBytes = 300L * 1024 * 1024;

    public async Task<bool> TryReserveAsync(long bytes, CancellationToken cancellationToken)
    {
        if (bytes <= 0) return true;
        var limit = configuration.GetValue("UploadQuota:DailyLimitBytes", DefaultLimitBytes);
        if (limit < 0) throw new InvalidOperationException("UploadQuota:DailyLimitBytes no puede ser negativo.");
        if (bytes > limit) return false;
        var date = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO upload_daily_usage (usage_date, used_bytes)
            VALUES ({date}, {bytes})
            ON CONFLICT (usage_date) DO UPDATE
            SET used_bytes = upload_daily_usage.used_bytes + EXCLUDED.used_bytes
            WHERE upload_daily_usage.used_bytes <= {limit - bytes}
            """, cancellationToken);
        return updated != 0;
    }
}
