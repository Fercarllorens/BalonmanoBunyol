using BalonmanoBunol.Domain.Social;
using BalonmanoBunol.Application.Storage;

namespace BalonmanoBunol.Application.Social;

public sealed class FeedService(IPostRepository posts, IPostMediaStorage mediaStorage, IUploadQuota uploadQuota, IStorageDeletionQueue deletionQueue) : IFeedService
{
    private const int MaxPostLength = 5000;
    private const int MaxAttachments = 4;
    private const long MaxImageLength = 10 * 1024 * 1024;
    private const long MaxVideoLength = 50 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, (string Extension, long MaxLength, string Kind)> AllowedMedia =
        new Dictionary<string, (string, long, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = (".jpg", MaxImageLength, "image"),
            ["image/png"] = (".png", MaxImageLength, "image"),
            ["image/webp"] = (".webp", MaxImageLength, "image"),
            ["video/mp4"] = (".mp4", MaxVideoLength, "video"),
            ["video/webm"] = (".webm", MaxVideoLength, "video")
        };

    public async Task<FeedPageDto> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, bool officialOnly, CancellationToken cancellationToken)
    {
        var boundedPageSize = Math.Clamp(pageSize, 1, 50);
        var results = await posts.GetLatestAsync(boundedPageSize + 1, before, beforeId, officialOnly, cancellationToken);
        var hasMore = results.Count > boundedPageSize;
        var page = results.Take(boundedPageSize).ToArray();
        var last = page.LastOrDefault();
        return new FeedPageDto(page, hasMore ? last?.CreatedAt : null, hasMore ? last?.Id : null, hasMore);
    }

    public async Task<FeedPostDto> CreateAsync(CreatePostCommand command, CancellationToken cancellationToken)
    {
        if (command.AuthorId == Guid.Empty) throw new ArgumentException("Se requiere una cuenta autenticada.");
        if (command.Attachments.Count > MaxAttachments) throw new ArgumentException("Se permiten como máximo cuatro adjuntos por publicación.");
        var content = command.Content?.Trim() ?? string.Empty;
        if (content.Length > MaxPostLength) throw new ArgumentException("El texto no puede superar los 5000 caracteres.");
        if (string.IsNullOrWhiteSpace(content) && command.Attachments.Count == 0) throw new ArgumentException("Añade texto o al menos un archivo.");

        var post = new Post(command.AuthorId, content);
        var attachmentRecords = new List<PostAttachment>();
        var savedStorageKeys = new List<string>();
        foreach (var attachment in command.Attachments)
        {
            var contentType = attachment.ContentType.Trim().ToLowerInvariant();
            if (!AllowedMedia.TryGetValue(contentType, out var media))
                throw new ArgumentException("Solo se admiten imágenes JPEG, PNG o WebP y vídeos MP4 o WebM.");
            if (attachment.Length <= 0 || attachment.Length > media.MaxLength)
                throw new ArgumentException(media.Kind == "image" ? "Cada imagen debe pesar 10 MB o menos." : "Cada vídeo debe pesar 50 MB o menos.");
            await ValidateContentSignatureAsync(attachment.Content, contentType, cancellationToken);
            if (attachment.Content.CanSeek) attachment.Content.Position = 0;
        }
        var totalBytes = command.Attachments.Sum(item => item.Length);
        if (totalBytes > 0 && !await uploadQuota.TryReserveAsync(totalBytes, cancellationToken)) throw new UploadQuotaExceededException();
        try
        {
            foreach (var attachment in command.Attachments)
            {
                var contentType = attachment.ContentType.Trim().ToLowerInvariant();
                var media = AllowedMedia[contentType];

                var attachmentId = Guid.NewGuid();
                if (attachment.Content.CanSeek) attachment.Content.Position = 0;
                var key = await mediaStorage.SaveAsync(attachmentId, media.Extension, attachment.Content, cancellationToken);
                savedStorageKeys.Add(key);
                attachmentRecords.Add(new PostAttachment(post.Id, key, attachment.FileName, contentType, attachment.Length));
            }

            await posts.AddAsync(post, attachmentRecords, cancellationToken);
            return ToDto(post, command.AuthorDisplayName, command.AuthorAvatarUrl, GetProfile(command.AuthorRoles), attachmentRecords);
        }
        catch
        {
            foreach (var key in savedStorageKeys)
            {
                try { await mediaStorage.DeleteAsync(key, CancellationToken.None); }
                catch
                {
                    try { await deletionQueue.EnqueueDurableAsync("feed", key, CancellationToken.None); }
                    catch { /* Sigue con el resto de claves y conserva la excepción original de la publicación. */ }
                }
            }
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid postId, Guid requesterId, CancellationToken cancellationToken)
    {
        var post = await posts.GetByIdAsync(postId, cancellationToken);
        if (post is null || post.AuthorId != requesterId || !post.IsPublished) return false;
        var storageKeys = await posts.GetAttachmentStorageKeysAsync(postId, cancellationToken);
        post.Delete();
        await posts.DeleteWithStorageCleanupAsync(post, storageKeys, cancellationToken);
        return true;
    }

    public async Task<(Stream Content, string ContentType, string FileName)?> OpenPublicAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await posts.GetPublicAttachmentAsync(attachmentId, cancellationToken);
        if (attachment is null) return null;
        var content = await mediaStorage.OpenReadAsync(attachment.Value.StorageKey, cancellationToken);
        return content is null ? null : (content, attachment.Value.ContentType, attachment.Value.FileName);
    }

    private static async Task ValidateContentSignatureAsync(Stream stream, string contentType, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek) throw new ArgumentException("No se pudo validar el archivo recibido.");

        var originalPosition = stream.Position;
        var header = new byte[16];
        var bytesRead = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        stream.Position = originalPosition;

        var valid = contentType switch
        {
            "image/jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => bytesRead >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "video/mp4" => bytesRead >= 8 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            "video/webm" => bytesRead >= 4 && header.AsSpan(0, 4).SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }),
            _ => false
        };

        if (!valid) throw new ArgumentException("El contenido del archivo no coincide con su formato declarado.");
    }

    private static FeedPostDto ToDto(Post post, string displayName, string? avatarUrl, string profile, IReadOnlyList<PostAttachment> attachments) => new(
        post.Id,
        post.AuthorId,
        displayName,
        avatarUrl,
        profile,
        post.TeamId,
        post.Content,
        post.CreatedAt,
        attachments.Select(attachment => new PostAttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.Length,
            $"/api/v1/feed/media/{attachment.Id}")).ToArray());

    private static string GetProfile(IReadOnlyList<string> roles)
    {
        if (roles.Contains("Administrador")) return "Administrador";
        if (roles.Contains("CuerpoTecnico") || roles.Contains("Entrenador") || roles.Contains("Staff")) return "Cuerpo Técnico";
        if (roles.Contains("Jugador")) return "Jugador/a";
        return "Socio / Afición";
    }
}
