namespace BalonmanoBunol.Application.Storage;

public interface IUploadQuota
{
    Task<bool> TryReserveAsync(long bytes, CancellationToken cancellationToken);
}

public sealed class UploadQuotaExceededException : Exception
{
    public UploadQuotaExceededException() : base("Se ha alcanzado el límite diario de subidas. Inténtalo de nuevo mañana.") { }
}
