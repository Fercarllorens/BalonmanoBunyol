using BalonmanoBunol.Application.Social;
using BalonmanoBunol.Domain.Social;
using BalonmanoBunol.Infrastructure.Social;
using BalonmanoBunol.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BalonmanoBunol.Tests;

public sealed class FeedServiceTests
{
    private readonly Mock<IPostRepository> _posts = new(MockBehavior.Strict);
    private readonly Mock<IPostMediaStorage> _media = new(MockBehavior.Strict);
    private readonly Mock<IUploadQuota> _quota = new(MockBehavior.Strict);
    private readonly Mock<IStorageDeletionQueue> _deletionQueue = new(MockBehavior.Strict);
    private FeedService CreateService()
    {
        _quota.Setup(quota => quota.TryReserveAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return new(_posts.Object, _media.Object, _quota.Object, _deletionQueue.Object);
    }

    [Fact]
    public async Task GetLatest_ClampsPageSizeAndReturnsCursorOnlyWhenThereAreMoreResults()
    {
        var date = DateTimeOffset.UtcNow;
        var items = Enumerable.Range(1, 4).Select(index => new FeedPostDto(Guid.NewGuid(), Guid.NewGuid(), "Autora", null, "Socio / Afición", null, $"Post {index}", date.AddMinutes(-index), [])).ToArray();
        _posts.Setup(repository => repository.GetLatestAsync(4, null, null, false, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var page = await CreateService().GetLatestAsync(3, null, null, false, CancellationToken.None);

        Assert.Equal(3, page.Posts.Count);
        Assert.True(page.HasMore);
        Assert.Equal(items[2].Id, page.NextBeforeId);
        Assert.Equal(items[2].CreatedAt, page.NextBefore);
        _posts.VerifyAll();
    }

    [Fact]
    public async Task GetLatest_ClampsPageSizeToRange()
    {
        _posts.Setup(repository => repository.GetLatestAsync(51, null, null, true, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<FeedPostDto>());

        var page = await CreateService().GetLatestAsync(500, null, null, true, CancellationToken.None);

        Assert.Empty(page.Posts);
        Assert.False(page.HasMore);
        Assert.Null(page.NextBefore);
        Assert.Null(page.NextBeforeId);
        _posts.VerifyAll();
    }

    [Fact]
    public async Task Create_RejectsMissingAuthorEmptyContentAndTooManyAttachments()
    {
        var service = CreateService();
        var author = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Command(Guid.Empty, "x"), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Command(author, " "), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Command(author, "text", [EmptyAttachment(), EmptyAttachment(), EmptyAttachment(), EmptyAttachment(), EmptyAttachment()]), CancellationToken.None));
        _posts.VerifyNoOtherCalls();
        _media.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("image/gif", 4, new byte[] { 0x47, 0x49, 0x46 })]
    [InlineData("image/jpeg", 10, new byte[] { 0x00, 0x01, 0x02 })]
    [InlineData("image/jpeg", 0, new byte[] { 0xFF, 0xD8, 0xFF })]
    public async Task Create_RejectsUnsupportedMimeInvalidLengthOrMismatchedSignature(string contentType, long length, byte[] header)
    {
        var attachment = Attachment(contentType, length, header);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().CreateAsync(Command(Guid.NewGuid(), "Foto", [attachment]), CancellationToken.None));

        _media.VerifyNoOtherCalls();
        _posts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_RejectsImageAndVideoOverTheirLimits()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Command(Guid.NewGuid(), null, [Attachment("image/png", 10 * 1024 * 1024 + 1, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])]), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Command(Guid.NewGuid(), null, [Attachment("video/mp4", 50 * 1024 * 1024 + 1, [0, 0, 0, 0, 0x66, 0x74, 0x79, 0x70])]), CancellationToken.None));
        _media.VerifyNoOtherCalls();
        _posts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_SavesValidImageAndMapsAuthorRole()
    {
        var authorId = Guid.NewGuid();
        var avatarUrl = $"/api/v1/profile/avatar/{authorId}";
        var attachment = Attachment("image/jpeg", 3, [0xFF, 0xD8, 0xFF]);
        _media.Setup(storage => storage.SaveAsync(It.IsAny<Guid>(), ".jpg", It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync("random.jpg");
        _posts.Setup(repository => repository.AddAsync(It.IsAny<Post>(), It.Is<IReadOnlyList<PostAttachment>>(items => items.Count == 1), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await CreateService().CreateAsync(Command(authorId, "  Hola  ", [attachment], ["Jugador"], avatarUrl), CancellationToken.None);

        Assert.Equal("Hola", result.Content);
        Assert.Equal("Jugador/a", result.AuthorProfile);
        Assert.Equal(avatarUrl, result.AuthorAvatarUrl);
        Assert.Single(result.Attachments);
        Assert.Equal("/api/v1/feed/media/" + result.Attachments[0].Id, result.Attachments[0].Url);
        _media.VerifyAll();
        _posts.VerifyAll();
    }

    [Fact]
    public async Task Create_DeletesStoredFilesWhenRepositorySaveFails()
    {
        var attachment = Attachment("image/png", 8, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        _media.Setup(storage => storage.SaveAsync(It.IsAny<Guid>(), ".png", It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync("stored.png");
        _media.Setup(storage => storage.DeleteAsync("stored.png", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _posts.Setup(repository => repository.AddAsync(It.IsAny<Post>(), It.IsAny<IReadOnlyList<PostAttachment>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("database unavailable"));

        await Assert.ThrowsAsync<DbUpdateException>(() => CreateService().CreateAsync(Command(Guid.NewGuid(), "Foto", [attachment]), CancellationToken.None));

        _media.VerifyAll();
        _posts.VerifyAll();
    }

    [Fact]
    public async Task Delete_OnlyDeletesActivePostsOwnedByRequester()
    {
        var authorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var post = new Post(authorId, "Post");
        _posts.Setup(repository => repository.GetAttachmentStorageKeysAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { "key1" });
        _deletionQueue.Setup(queue => queue.EnqueueAsync("feed", "key1", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _posts.Setup(repository => repository.DeleteWithStorageCleanupAsync(post, It.Is<IReadOnlyList<string>>(keys => keys.SequenceEqual(new[] { "key1" })), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _posts.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid requestedId, CancellationToken _) => Task.FromResult<Post?>(requestedId == post.Id ? post : null));
        _posts.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = CreateService();

        Assert.False(await service.DeleteAsync(Guid.NewGuid(), authorId, CancellationToken.None));
        Assert.False(await service.DeleteAsync(post.Id, otherId, CancellationToken.None));
        Assert.True(await service.DeleteAsync(post.Id, authorId, CancellationToken.None));
        Assert.False(await service.DeleteAsync(post.Id, authorId, CancellationToken.None));

        Assert.False(post.IsPublished);
        Assert.NotNull(post.DeletedAt);
        _posts.Verify(repository => repository.DeleteWithStorageCleanupAsync(post, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreatePostCommand Command(Guid authorId, string? content, IReadOnlyList<CreatePostAttachment>? attachments = null, IReadOnlyList<string>? roles = null, string? avatarUrl = null) =>
        new(authorId, "Autora", roles ?? [], content, attachments ?? [], avatarUrl);

    private static CreatePostAttachment EmptyAttachment() => Attachment("image/jpeg", 1, [0xFF, 0xD8, 0xFF]);

    private static CreatePostAttachment Attachment(string contentType, long length, byte[] bytes) =>
        new("archivo.bin", contentType, length, new MemoryStream(bytes));
}
