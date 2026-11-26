using BalonmanoBunol.Infrastructure.Social;
using BalonmanoBunol.Infrastructure.Profile;
using BalonmanoBunol.Infrastructure.Teams;
using BalonmanoBunol.Infrastructure.Persistence;
using BalonmanoBunol.Infrastructure;
using Amazon.S3;
using Amazon.S3.Model;
using Moq;

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

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    public async Task LocalProfileAndTeamStorage_SaveOpenDelete(string extension)
    {
        var roots = new[]
        {
            Path.Combine(Path.GetTempPath(), "balonmano-tests", Guid.NewGuid().ToString("N")),
            Path.Combine(Path.GetTempPath(), "balonmano-tests", Guid.NewGuid().ToString("N")),
        };
        var avatarStorage = new LocalProfileAvatarStorage(roots[0]);
        var boardStorage = new LocalTeamBoardStorage(roots[1]);
        var savedKeys = new List<string>();
        var storages = new (Func<Guid, string, Stream, CancellationToken, Task<string>> Save,
            Func<string, CancellationToken, Task<Stream?>> Open,
            Func<string, CancellationToken, Task> Delete)[]
        {
            (avatarStorage.SaveAsync, avatarStorage.OpenReadAsync, avatarStorage.DeleteAsync),
            (boardStorage.SaveAsync, boardStorage.OpenReadAsync, boardStorage.DeleteAsync),
        };
        var bytes = new byte[] { 1, 2, 3, 4 };
        try
        {
            foreach (var storage in storages)
            {
                var key = await storage.Save(Guid.NewGuid(), extension, new MemoryStream(bytes), CancellationToken.None);
                savedKeys.Add(key);
                await using (var saved = await storage.Open(key, CancellationToken.None))
                {
                    Assert.NotNull(saved);
                    using var result = new MemoryStream();
                    await saved!.CopyToAsync(result);
                    Assert.Equal(bytes, result.ToArray());
                }
                await storage.Delete(key, CancellationToken.None);
                Assert.Null(await storage.Open(key, CancellationToken.None));
            }
        }
        finally
        {
            foreach (var key in savedKeys) await avatarStorage.DeleteAsync(key, CancellationToken.None);
            foreach (var key in savedKeys) await boardStorage.DeleteAsync(key, CancellationToken.None);
            foreach (var root in roots)
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void AddInfrastructure_R2Provider_RequiresAllCredentials()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new R2ObjectStorageConfiguration(
            "R2", null, "bucket", "access", "secret").Validate());
        Assert.Contains("R2:Endpoint", exception.Message);
    }

    [Theory]
    [InlineData(null, "bucket", "access", "secret")]
    [InlineData("https://account.r2.cloudflarestorage.com", null, "access", "secret")]
    [InlineData("https://account.r2.cloudflarestorage.com", "bucket", null, "secret")]
    [InlineData("https://account.r2.cloudflarestorage.com", "bucket", "access", null)]
    public void R2Configuration_RequiresAllSettings(string? endpoint, string? bucket, string? accessKeyId, string? secretAccessKey)
    {
        var configuration = new R2ObjectStorageConfiguration("R2", endpoint, bucket, accessKeyId, secretAccessKey);
        Assert.Throws<InvalidOperationException>(configuration.Validate);
    }

    [Fact]
    public void LocalStorageConfiguration_DoesNotRequireR2Settings() =>
        new R2ObjectStorageConfiguration("Local", null, null, null, null).Validate();

    [Fact]
    public async Task R2ObjectStorage_SaveOpenDelete_UsesPrivateObjectOperations()
    {
        var savedBytes = new byte[] { 4, 5, 6 };
        GetObjectResponse? savedResponse = null;
        var client = new Mock<IAmazonS3>(MockBehavior.Strict);
        var objectKey = $"club-bunol/feed/{Guid.NewGuid():N}.jpg";
        client.Setup(storage => storage.GetObjectAsync("club-assets", objectKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedResponse = CreateObjectResponse(savedBytes));
        client.Setup(storage => storage.DeleteObjectAsync(It.Is<DeleteObjectRequest>(request =>
            request.BucketName == "club-assets" && request.Key == objectKey), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());
        var storage = new R2ObjectStorage(client.Object, "club-assets", "club-bunol/feed/");

        await using (var read = await storage.OpenReadAsync(objectKey, CancellationToken.None))
        {
            Assert.NotNull(read);
            using var copy = new MemoryStream();
            await read!.CopyToAsync(copy);
            Assert.Equal(savedBytes, copy.ToArray());
        }
        await storage.DeleteAsync(objectKey, CancellationToken.None);
        client.Verify(storage => storage.GetObjectAsync("club-assets", objectKey, It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(storage => storage.DeleteObjectAsync(It.Is<DeleteObjectRequest>(request => request.Key == objectKey), It.IsAny<CancellationToken>()), Times.Once);
        client.VerifyNoOtherCalls();
        Assert.NotNull(savedResponse);
    }

    private static GetObjectResponse CreateObjectResponse(byte[] bytes) => new()
    {
        ResponseStream = new MemoryStream(bytes),
    };

    [Fact]
    public async Task R2ObjectStorage_UsesCollectionPrefixAndRejectsCrossCollectionKeys()
    {
        var client = new Mock<IAmazonS3>(MockBehavior.Strict);
        client.Setup(storage => storage.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        var storage = new R2ObjectStorage(client.Object, "club-assets", "club-bunol/feed/");
        var id = Guid.NewGuid();

        var key = await storage.SaveAsync(id, ".jpg", new MemoryStream([1, 2, 3]), CancellationToken.None);

        Assert.Equal($"club-bunol/feed/{id:N}.jpg", key);
        client.Verify(s3 => s3.PutObjectAsync(It.Is<PutObjectRequest>(request =>
            request.BucketName == "club-assets" && request.Key == key), It.IsAny<CancellationToken>()), Times.Once);
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.DeleteAsync("club-bunol/avatars/other.jpg", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.OpenReadAsync("club-bunol/feed/../secret.jpg", CancellationToken.None));
        client.VerifyNoOtherCalls();
    }
}
