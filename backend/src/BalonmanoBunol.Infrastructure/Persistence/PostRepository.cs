using BalonmanoBunol.Application.Social;
using Microsoft.EntityFrameworkCore;

namespace BalonmanoBunol.Infrastructure.Persistence;

public sealed class PostRepository(ClubDbContext dbContext) : IPostRepository
{
    public async Task<IReadOnlyList<FeedPostDto>> GetLatestAsync(int pageSize, DateTimeOffset? before, Guid? beforeId, bool officialOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Posts.AsNoTracking().Where(post => post.IsPublished && post.DeletedAt == null);
        if (officialOnly)
        {
            query = query.Where(post => dbContext.UserRoles.Join(dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (userRole, role) => new { userRole.UserId, role.Name })
                .Any(userRole => userRole.UserId == post.AuthorId && (userRole.Name == "Administrador" || userRole.Name == "CuerpoTecnico" || userRole.Name == "Entrenador" || userRole.Name == "Staff")));
        }
        if (before is not null && beforeId is not null)
        {
            query = query.Where(post => post.CreatedAt < before.Value || (post.CreatedAt == before.Value && post.Id.CompareTo(beforeId.Value) < 0));
        }
        else if (before is not null)
        {
            query = query.Where(post => post.CreatedAt < before.Value);
        }

        var result = await query
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var authorIds = result.Select(post => post.AuthorId).Distinct().ToArray();
        var authors = await dbContext.Users.AsNoTracking().Where(user => authorIds.Contains(user.Id))
            .Select(user => new { user.Id, user.DisplayName, user.AvatarStorageKey }).ToDictionaryAsync(user => user.Id, user => new { user.DisplayName, user.AvatarStorageKey }, cancellationToken);
        var authorRoles = await (from userRole in dbContext.UserRoles
                                 join role in dbContext.Roles on userRole.RoleId equals role.Id
                                 where authorIds.Contains(userRole.UserId)
                                 select new { userRole.UserId, role.Name }).ToListAsync(cancellationToken);
        var postIds = result.Select(post => post.Id).ToArray();
        var postAttachments = await dbContext.PostAttachments.AsNoTracking().Where(attachment => postIds.Contains(attachment.PostId))
            .OrderBy(attachment => attachment.CreatedAt).ToListAsync(cancellationToken);

        return result.Select(post => new FeedPostDto(
            post.Id,
            post.AuthorId,
            authors.TryGetValue(post.AuthorId, out var author) ? author.DisplayName : "Usuario del club",
            authors.TryGetValue(post.AuthorId, out author) && author.AvatarStorageKey is not null ? $"/api/v1/feed/author-avatar/{post.AuthorId}" : null,
            GetProfile(authorRoles.Where(item => item.UserId == post.AuthorId).Select(item => item.Name ?? string.Empty)),
            post.TeamId,
            post.Content,
            post.CreatedAt,
            postAttachments.Where(attachment => attachment.PostId == post.Id)
                .Select(attachment => new PostAttachmentDto(attachment.Id, attachment.FileName, attachment.ContentType, attachment.Length, $"/api/v1/feed/media/{attachment.Id}"))
                .ToArray())).ToArray();
    }

    public async Task AddAsync(BalonmanoBunol.Domain.Social.Post post, IReadOnlyList<BalonmanoBunol.Domain.Social.PostAttachment> attachments, CancellationToken cancellationToken)
    {
        dbContext.Posts.Add(post);
        dbContext.PostAttachments.AddRange(attachments);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<BalonmanoBunol.Domain.Social.Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken) =>
        dbContext.Posts.SingleOrDefaultAsync(post => post.Id == postId, cancellationToken);

    public async Task<(string StorageKey, string ContentType, string FileName)?> GetPublicAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.PostAttachments.AsNoTracking()
            .Join(dbContext.Posts.AsNoTracking().Where(post => post.IsPublished && post.DeletedAt == null), attachment => attachment.PostId, post => post.Id, (attachment, _) => attachment)
            .Where(attachment => attachment.Id == attachmentId)
            .Select(attachment => new { attachment.StorageKey, attachment.ContentType, attachment.FileName })
            .SingleOrDefaultAsync(cancellationToken);
        return attachment is null ? null : (attachment.StorageKey, attachment.ContentType, attachment.FileName);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private static string GetProfile(IEnumerable<string> roles)
    {
        var roleList = roles.ToArray();
        if (roleList.Contains("Administrador")) return "Administrador";
        if (roleList.Any(role => role is "CuerpoTecnico" or "Entrenador" or "Staff")) return "Cuerpo Técnico";
        if (roleList.Contains("Jugador")) return "Jugador/a";
        return "Socio / Afición";
    }
}
