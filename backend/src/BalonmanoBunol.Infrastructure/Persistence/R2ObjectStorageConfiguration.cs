namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed record R2ObjectStorageConfiguration(
    string? Provider,
    string? Endpoint,
    string? Bucket,
    string? AccessKeyId,
    string? SecretAccessKey)
{
    public bool IsEnabled => string.Equals(Provider, "R2", StringComparison.OrdinalIgnoreCase);

    public void Validate()
    {
        if (!IsEnabled) return;
        if (string.IsNullOrWhiteSpace(Endpoint)) throw new InvalidOperationException("FileStorage:Provider=R2 requiere R2:Endpoint.");
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpointUri) || endpointUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("R2:Endpoint debe ser una URL HTTPS válida.");
        if (string.IsNullOrWhiteSpace(Bucket)) throw new InvalidOperationException("FileStorage:Provider=R2 requiere R2:Bucket.");
        if (string.IsNullOrWhiteSpace(AccessKeyId)) throw new InvalidOperationException("FileStorage:Provider=R2 requiere R2:AccessKeyId.");
        if (string.IsNullOrWhiteSpace(SecretAccessKey)) throw new InvalidOperationException("FileStorage:Provider=R2 requiere R2:SecretAccessKey.");
    }
}
