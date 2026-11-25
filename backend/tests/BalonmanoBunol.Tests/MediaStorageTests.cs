using BalonmanoBunol.Infrastructure.Social;

namespace BalonmanoBunol.Tests;

public sealed class MediaStorageTests
{
    [Fact]
    public async Task SaveOpenAndDelete_UsesStorageRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "balonmano-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalPostMediaStorage(root);
        var id = Guid.NewGuid();
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0x01 };

        try
        {
            var key = await storage.SaveAsync(id, ".jpg", new MemoryStream(bytes), CancellationToken.None);
            Assert.Equal(id.ToString("N") + ".jpg", key);

            await using (var saved = await storage.OpenReadAsync(key, CancellationToken.None))
            {
                Assert.NotNull(saved);
                var result = new MemoryStream();
                await saved!.CopyToAsync(result);
                Assert.Equal(bytes, result.ToArray());
            }

            await storage.DeleteAsync(key, CancellationToken.None);
            Assert.Null(await storage.OpenReadAsync(key, CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../outside.jpg")]
    [InlineData("subfolder/file.jpg")]
    [InlineData("..")]
    public async Task OpenRead_RejectsPathTraversal(string key)
    {
        var root = Path.Combine(Path.GetTempPath(), "balonmano-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalPostMediaStorage(root);

        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.OpenReadAsync(key, CancellationToken.None));
    }

    [Fact]
    public async Task Save_UsesNonOverwritingFileCreation()
    {
        var root = Path.Combine(Path.GetTempPath(), "balonmano-tests", Guid.NewGuid().ToString("N"));
        var storage = new LocalPostMediaStorage(root);
        var id = Guid.NewGuid();
        try
        {
            await storage.SaveAsync(id, ".jpg", new MemoryStream([1]), CancellationToken.None);
            await Assert.ThrowsAsync<IOException>(() => storage.SaveAsync(id, ".jpg", new MemoryStream([2]), CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
