using BalonmanoBunol.Application.Profile;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class LocalProfileAvatarStorage(string rootPath) : IProfileAvatarStorage
{
    private readonly string _rootPath = Path.GetFullPath(rootPath);

    public async Task<string> SaveAsync(Guid avatarId, string extension, Stream content, CancellationToken cancellationToken)
    {
        var storageKey = $"{avatarId:N}{extension}";
        var path = GetSafePath(storageKey);
        Directory.CreateDirectory(_rootPath);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(target, cancellationToken);
        return storageKey;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetSafePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetSafePath(storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null;
        return Task.FromResult(stream);
    }

    private string GetSafePath(string storageKey)
    {
        if (Path.GetFileName(storageKey) != storageKey) throw new InvalidOperationException("Clave de almacenamiento no válida.");
        var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        if (!path.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Clave de almacenamiento fuera de la carpeta permitida.");
        return path;
    }
}
