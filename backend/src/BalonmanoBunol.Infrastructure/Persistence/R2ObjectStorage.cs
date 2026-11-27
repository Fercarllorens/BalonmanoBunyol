using Amazon.S3;
using Amazon.S3.Model;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class R2ObjectStorage(IAmazonS3 client, string bucketName, string keyPrefix)
{
    public async Task<string> SaveAsync(Guid id, string extension, Stream content, CancellationToken cancellationToken)
    {
        var key = $"{keyPrefix}{id:N}{extension}";
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        }, cancellationToken);
        return key;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        var key = GetKey(storageKey);
        try
        {
            await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucketName, Key = key }, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Borrado idempotente para permitir compensar cargas parciales.
        }
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var key = GetKey(storageKey);
        try
        {
            var response = await client.GetObjectAsync(bucketName, key, cancellationToken);
            return new ObjectResponseStream(response);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private string GetKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains('\\') || storageKey.StartsWith('/') || storageKey.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Clave de almacenamiento no válida.");
        if (!storageKey.StartsWith(keyPrefix, StringComparison.Ordinal)
            || storageKey.Length == keyPrefix.Length
            || storageKey.AsSpan(keyPrefix.Length).Contains('/'))
            throw new InvalidOperationException("La clave de almacenamiento no pertenece a esta colección.");
        var objectName = storageKey[(keyPrefix.Length)..];
        if (objectName.Contains('.')
            && (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(objectName), "N", out _)
                || Path.GetExtension(objectName) is not (".jpg" or ".png" or ".webp" or ".mp4" or ".webm" or ".pdf")))
            throw new InvalidOperationException("Clave de almacenamiento no válida.");
        return storageKey;
    }

    private sealed class ObjectResponseStream(GetObjectResponse response) : Stream
    {
        private readonly Stream _inner = response.ResponseStream;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) response.Dispose();
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            response.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
