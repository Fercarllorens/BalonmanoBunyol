namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class StorageDeletionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StorageKind { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;
    public string? LastError { get; set; }
}
