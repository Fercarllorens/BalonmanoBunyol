using BalonmanoBunol.Application.Social;
using BalonmanoBunol.Application.Auth;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using BalonmanoBunol.Infrastructure.Persistence;
using BalonmanoBunol.Application.Profile;
using BalonmanoBunol.Application.Administration;
using BalonmanoBunol.Application.Teams;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("ClubDatabase")))
{
    throw new InvalidOperationException("Falta ConnectionStrings__ClubDatabase. Inicia la API con npm run api para cargar la configuración local desde .env.");
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "cb-bunol-csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 201L * 1024 * 1024);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

var app = builder.Build();

await InitializeDatabaseAsync(app);

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/auth") || context.Request.Path.StartsWithSegments("/api/v1/admin") || context.Request.Path.StartsWithSegments("/api/v1/profile") || context.Request.Path.StartsWithSegments("/api/v1/feed/author-avatar") || context.Request.Path.StartsWithSegments("/api/v1/teams") || context.Request.Path.StartsWithSegments("/api/v1/my-teams"))
        context.Response.Headers.CacheControl = "no-store, no-cache";

    if (context.Request.Path.StartsWithSegments("/api/v1/admin") && context.User.Identity?.IsAuthenticated == true)
    {
        var userIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userManager = context.RequestServices.GetRequiredService<UserManager<ClubUser>>();
        var currentUser = Guid.TryParse(userIdValue, out var currentUserId)
            ? await userManager.FindByIdAsync(currentUserId.ToString())
            : null;
        if (currentUser is null || !currentUser.IsActive || !await userManager.IsInRoleAsync(currentUser, "Administrador"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }

    var isProtectedMutation = (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
        && (context.Request.Path.StartsWithSegments("/api/v1/auth/register")
            || context.Request.Path.StartsWithSegments("/api/v1/auth/login")
            || context.Request.Path.StartsWithSegments("/api/v1/auth/logout")
            || context.Request.Path.StartsWithSegments("/api/v1/profile")
            || context.Request.Path.StartsWithSegments("/api/v1/admin")
            || context.Request.Path.StartsWithSegments("/api/v1/teams")
            || context.Request.Path.StartsWithSegments("/api/v1/feed"));

    if (isProtectedMutation)
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "La solicitud de seguridad ha caducado. Recarga la página e inténtalo de nuevo." });
            return;
        }
    }

    await next();
});

app.MapGet("/api/v1/auth/csrf", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Headers.CacheControl = "no-store, no-cache";
    return Results.Ok(new { token = tokens.RequestToken });
}).AllowAnonymous();

app.MapPost("/api/v1/auth/register", async (RegisterRequest request, IAuthService auth, CancellationToken cancellationToken) =>
{
    var result = await auth.RegisterAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.User) : Results.BadRequest(new { message = result.Error });
}).AllowAnonymous();

app.MapPost("/api/v1/auth/login", async (LoginRequest request, IAuthService auth, CancellationToken cancellationToken) =>
{
    var result = await auth.LoginAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.User) : Results.Json(new { message = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
}).AllowAnonymous();

app.MapGet("/api/v1/auth/me", async (HttpContext context, IAuthService auth, CancellationToken cancellationToken) =>
{
    var userIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdValue, out var userId)) return Results.Unauthorized();
    var user = await auth.GetCurrentUserAsync(userId, cancellationToken);
    return user is null ? Results.Unauthorized() : Results.Ok(user);
}).RequireAuthorization();

app.MapPost("/api/v1/auth/logout", async (IAuthService auth, CancellationToken cancellationToken) =>
{
    await auth.LogoutAsync(cancellationToken);
    return Results.NoContent();
}).RequireAuthorization();

var profile = app.MapGroup("/api/v1/profile").RequireAuthorization();
profile.MapGet("/", async (HttpContext context, IProfileService profiles, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Results.Unauthorized();
    var result = await profiles.GetAsync(userId, cancellationToken);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});
profile.MapPut("/", async (UpdateProfileRequest request, HttpContext context, IProfileService profiles, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Results.Unauthorized();
    var result = await profiles.UpdateAsync(userId, request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Profile) : Results.BadRequest(new { message = result.Error });
});
app.MapPost("/api/v1/profile/avatar", async (HttpContext context, IProfileService profiles, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Results.Unauthorized();
    if (context.Request.ContentLength is > 5_300_000) return Results.BadRequest(new { message = "La imagen debe pesar 5 MB o menos." });
    var requestSizeFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
    if (requestSizeFeature is { IsReadOnly: false }) requestSizeFeature.MaxRequestBodySize = 5_300_000;
    IFormCollection form;
    try { form = await context.Request.ReadFormAsync(cancellationToken); }
    catch (InvalidDataException) { return Results.BadRequest(new { message = "El archivo supera el tamaño máximo permitido." }); }
    var file = form.Files.GetFile("avatar");
    if (file is null) return Results.BadRequest(new { message = "Selecciona una imagen de perfil." });
    if (file.Length > 5 * 1024 * 1024) return Results.BadRequest(new { message = "La imagen debe pesar 5 MB o menos." });
    await using var stream = file.OpenReadStream();
    var result = await profiles.SetAvatarAsync(userId, file.ContentType, file.Length, stream, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Profile) : Results.BadRequest(new { message = result.Error });
}).RequireAuthorization();
profile.MapDelete("/avatar", async (HttpContext context, IProfileService profiles, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Results.Unauthorized();
    return await profiles.RemoveAvatarAsync(userId, cancellationToken) ? Results.NoContent() : Results.NotFound();
});
app.MapGet("/api/v1/profile/avatar/{userId:guid}", async (Guid userId, IProfileService profiles, CancellationToken cancellationToken) =>
{
    var avatar = await profiles.OpenAvatarAsync(userId, cancellationToken);
    return avatar is null
        ? Results.NotFound()
        : Results.File(avatar.Value.Content, avatar.Value.ContentType, enableRangeProcessing: false);
}).RequireAuthorization();

var roleRequests = app.MapGroup("/api/v1/admin/role-requests").RequireAuthorization(policy => policy.RequireRole("Administrador"));
roleRequests.MapGet("/", async (bool? pendingOnly, IAuthService auth, CancellationToken cancellationToken) =>
{
    var requests = await auth.GetRoleRequestsAsync(cancellationToken);
    var selected = pendingOnly == true ? requests.Where(request => request.Status == "Pending") : requests;
    return Results.Ok(selected.Select(request => new
    {
        id = request.Id,
        displayName = request.DisplayName,
        email = request.Email,
        requestedRole = request.RequestedRole,
        status = request.Status,
        createdAt = request.CreatedAt,
        resolvedAt = request.ResolvedAt,
        resolvedByUserId = request.ResolvedByUserId
    }));
});

var administration = app.MapGroup("/api/v1/admin").RequireAuthorization(policy => policy.RequireRole("Administrador"));
administration.MapGet("/users", async (string? query, bool? coachesOnly, bool? playersOnly, IAdministrationService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.SearchUsersAsync(query ?? string.Empty, coachesOnly == true, playersOnly == true, cancellationToken)));
administration.MapGet("/teams", async (IAdministrationService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetTeamsAsync(cancellationToken)));
administration.MapGet("/teams/{teamId:guid}/players", async (Guid teamId, IAdministrationService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetTeamPlayersAsync(teamId, cancellationToken)));
administration.MapPut("/teams/{teamId:guid}/players/{playerId:guid}", async (Guid teamId, Guid playerId, TeamPlayerInput input, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.AssignPlayerAsync(administratorId, playerId,
        new AdminPlayerAssignmentInput(teamId, input.JerseyNumber, input.Position), cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
administration.MapDelete("/teams/{teamId:guid}/players/{playerId:guid}", async (Guid teamId, Guid playerId, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.RemovePlayerAsync(administratorId, playerId, teamId, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
administration.MapPost("/teams", async (TeamInput input, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.CreateTeamAsync(administratorId, input, cancellationToken);
    return result.Succeeded ? Results.Created("/api/v1/admin/teams", null) : Results.BadRequest(new { message = result.Error });
});
administration.MapPut("/teams/{teamId:guid}", async (Guid teamId, TeamInput input, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.UpdateTeamAsync(administratorId, teamId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
administration.MapPut("/teams/{teamId:guid}/active", async (Guid teamId, bool isActive, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.SetTeamActiveAsync(administratorId, teamId, isActive, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
administration.MapPut("/users/{userId:guid}/teams", async (Guid userId, AssignTeamsInput input, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.AssignTeamsAsync(administratorId, userId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
administration.MapPut("/users/{userId:guid}/player-assignment", async (Guid userId, AdminPlayerAssignmentInput input, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.AssignPlayerAsync(administratorId, userId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
administration.MapDelete("/users/{userId:guid}/player-assignment/{teamId:guid}", async (Guid userId, Guid teamId, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.RemovePlayerAsync(administratorId, userId, teamId, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
administration.MapPut("/users/{userId:guid}/role", async (Guid userId, ChangeRoleInput input, HttpContext context, IAdministrationService service, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await service.ChangeRoleAsync(administratorId, userId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
app.MapGet("/api/v1/my-teams", async (HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Results.Unauthorized();
    return Results.Ok(await teams.GetMyTeamsAsync(userId, cancellationToken));
}).RequireAuthorization();
var teamSpaces = app.MapGroup("/api/v1/teams/{teamId:guid}").RequireAuthorization();
teamSpaces.MapGet("/space", async (Guid teamId, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var space = await teams.GetTeamAsync(userId, teamId, cancellationToken);
    return space is null ? Results.NotFound() : Results.Ok(space);
});
teamSpaces.MapGet("/calendar", async (Guid teamId, int? year, int? month, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var now = DateTime.UtcNow;
    try
    {
        var calendar = await teams.GetCalendarAsync(userId, teamId, year ?? now.Year, month ?? now.Month, cancellationToken);
        return calendar is null ? Results.NotFound() : Results.Ok(calendar);
    }
    catch (ArgumentOutOfRangeException) { return Results.BadRequest(new { message = "El mes indicado no es válido." }); }
});
teamSpaces.MapGet("/players/search", async (Guid teamId, string? query, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    return Results.Ok(await teams.SearchPlayersAsync(userId, teamId, query ?? string.Empty, cancellationToken));
});
teamSpaces.MapPost("/players", async (Guid teamId, TeamPlayerInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.AddPlayerAsync(userId, teamId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapPut("/players/{playerId:guid}", async (Guid teamId, Guid playerId, TeamPlayerStatsInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.UpdatePlayerStatsAsync(userId, teamId, input with { UserId = playerId }, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapDelete("/players/{playerId:guid}", async (Guid teamId, Guid playerId, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.RemovePlayerAsync(userId, teamId, playerId, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapPost("/matches", async (Guid teamId, TeamMatchInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.CreateMatchAsync(userId, teamId, input, cancellationToken);
    return result.Result.Succeeded ? Results.Created($"/api/v1/teams/{teamId}/space", new { id = result.MatchId }) : Results.BadRequest(new { message = result.Result.Error });
});
teamSpaces.MapPut("/matches/{matchId:guid}/attendance", async (Guid teamId, Guid matchId, TeamAttendanceInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.SetAttendanceAsync(userId, teamId, matchId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapPost("/events", async (Guid teamId, TeamEventInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.CreateEventAsync(userId, teamId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapPut("/events/{eventId:guid}", async (Guid teamId, Guid eventId, TeamEventInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.UpdateEventAsync(userId, teamId, eventId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapDelete("/events/{eventId:guid}", async (Guid teamId, Guid eventId, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.DeleteEventAsync(userId, teamId, eventId, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapPut("/training-plan", async (Guid teamId, TeamTrainingPlanInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.SetTrainingPlanAsync(userId, teamId, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapPut("/training-plan/{slotId:guid}/exceptions/{date}", async (Guid teamId, Guid slotId, DateOnly date, TeamTrainingExceptionInput input, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.SetTrainingExceptionAsync(userId, teamId, slotId, date, input, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapDelete("/training-plan/{slotId:guid}/exceptions/{date}", async (Guid teamId, Guid slotId, DateOnly date, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var result = await teams.DeleteTrainingExceptionAsync(userId, teamId, slotId, date, cancellationToken);
    return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
});
teamSpaces.MapPost("/board", async (Guid teamId, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    if (context.Request.ContentLength is > 105_000_000) return Results.BadRequest(new { message = "Los adjuntos superan el límite total de 100 MB." });
    IFormCollection form;
    try { form = await context.Request.ReadFormAsync(cancellationToken); }
    catch (InvalidDataException) { return Results.BadRequest(new { message = "El tamaño o formato de la solicitud no es válido." }); }
    if (form.Files.Count > 4) return Results.BadRequest(new { message = "Se permiten hasta cuatro archivos por mensaje." });
    var files = new List<TeamBoardFile>();
    foreach (var file in form.Files)
    {
        if (file.Length > 100 * 1024 * 1024) return Results.BadRequest(new { message = "Cada archivo debe pesar 100 MB o menos." });
        var stream = file.OpenReadStream();
        files.Add(new TeamBoardFile(file.FileName, file.ContentType, file.Length, stream));
    }
    try
    {
        var result = await teams.CreateBoardPostAsync(userId, teamId, new TeamBoardInput(form["content"].ToString()), files, cancellationToken);
        return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { message = result.Error });
    }
    finally
    {
        foreach (var file in files) await file.Content.DisposeAsync();
    }
}).RequireAuthorization();
app.MapGet("/api/v1/teams/{teamId:guid}/board-files/{attachmentId:guid}", async (Guid teamId, Guid attachmentId, HttpContext context, ITeamSpaceService teams, CancellationToken cancellationToken) =>
{
    var userId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    var file = await teams.OpenBoardAttachmentAsync(userId, teamId, attachmentId, cancellationToken);
    return file is null
        ? Results.NotFound()
        : Results.File(file.Value.Content, file.Value.ContentType,
            file.Value.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || file.Value.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                ? null
                : file.Value.FileName,
            enableRangeProcessing: file.Value.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));
}).RequireAuthorization();
roleRequests.MapPost("/{requestId:guid}/approve", async (Guid requestId, HttpContext context, IAuthService auth, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    return await auth.ResolveRoleRequestAsync(requestId, administratorId, approve: true, cancellationToken)
        ? Results.NoContent()
        : Results.Conflict(new { message = "La solicitud no existe o ya ha sido resuelta." });
});
roleRequests.MapPost("/{requestId:guid}/reject", async (Guid requestId, HttpContext context, IAuthService auth, CancellationToken cancellationToken) =>
{
    var administratorId = Guid.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    return await auth.ResolveRoleRequestAsync(requestId, administratorId, approve: false, cancellationToken)
        ? Results.NoContent()
        : Results.Conflict(new { message = "La solicitud no existe o ya ha sido resuelta." });
});

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "balonmano-bunol-api",
    timestamp = DateTimeOffset.UtcNow
})).WithName("HealthCheck");

app.MapGet("/api", () => Results.Ok(new
{
    name = "Club Balonmano Buñol API",
    version = "v1",
    documentation = "/docs"
}));

app.MapGet("/api/v1/feed", async (int? pageSize, DateTimeOffset? before, Guid? beforeId, IFeedService feed, CancellationToken cancellationToken) =>
    Results.Ok(await feed.GetLatestAsync(pageSize ?? 20, before, beforeId, officialOnly: false, cancellationToken)))
    .WithName("GetFeed");

app.MapGet("/api/v1/feed/announcements", async (int? pageSize, DateTimeOffset? before, Guid? beforeId, IFeedService feed, CancellationToken cancellationToken) =>
    Results.Ok(await feed.GetLatestAsync(pageSize ?? 20, before, beforeId, officialOnly: true, cancellationToken)))
    .WithName("GetOfficialFeed");

app.MapPost("/api/v1/feed", async (HttpContext context, IAntiforgery antiforgery, IFeedService feed, CancellationToken cancellationToken) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "La solicitud de seguridad ha caducado. Recarga la página." }); }

    const long maxRequestBytes = 201L * 1024 * 1024;
    if (context.Request.ContentLength is null)
        return Results.BadRequest(new { message = "No se ha podido determinar el tamaño total de los adjuntos." });
    if (context.Request.ContentLength > maxRequestBytes)
        return Results.BadRequest(new { message = "El tamaño total de los adjuntos supera el límite permitido." });

    var userIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdValue, out var authorId)) return Results.Unauthorized();
    var form = await context.Request.ReadFormAsync(cancellationToken);
    if (form.Files.Count > 4) return Results.BadRequest(new { message = "Se permiten como máximo cuatro adjuntos por publicación." });
    if (form.Files.Any(file => file.Length > 50L * 1024 * 1024)) return Results.BadRequest(new { message = "Cada vídeo debe pesar 50 MB o menos." });

    var streams = form.Files.Select(file => file.OpenReadStream()).ToArray();
    try
    {
        var attachments = form.Files.Select((file, index) => new CreatePostAttachment(file.FileName, file.ContentType, file.Length, streams[index])).ToArray();
        var authorName = context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Usuario del club";
        var userManager = context.RequestServices.GetRequiredService<UserManager<ClubUser>>();
        var authenticatedUser = await userManager.FindByIdAsync(authorId.ToString());
        if (authenticatedUser is null || !authenticatedUser.IsActive) return Results.Unauthorized();
        var authorRoles = (await userManager.GetRolesAsync(authenticatedUser)).ToArray();
        var authorAvatarUrl = authenticatedUser.AvatarStorageKey is null ? null : $"/api/v1/feed/author-avatar/{authorId}";
        var post = await feed.CreateAsync(new CreatePostCommand(authorId, authorName, authorRoles, form["content"].ToString(), attachments, authorAvatarUrl), cancellationToken);
        return Results.Created($"/api/v1/feed/{post.Id}", post);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { message = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { message = exception.Message });
    }
    finally
    {
        foreach (var stream in streams) await stream.DisposeAsync();
    }
}).RequireAuthorization();

app.MapGet("/api/v1/feed/author-avatar/{authorId:guid}", async (Guid authorId, HttpContext context, ClubDbContext db, IProfileService profiles, CancellationToken cancellationToken) =>
{
    var canView = await db.Posts.AsNoTracking().AnyAsync(
        post => post.AuthorId == authorId && post.IsPublished && post.DeletedAt == null,
        cancellationToken);
    if (!canView) return Results.NotFound();
    var avatar = await profiles.OpenAvatarAsync(authorId, cancellationToken);
    return avatar is null
        ? Results.NotFound()
        : Results.File(avatar.Value.Content, avatar.Value.ContentType, enableRangeProcessing: false);
}).AllowAnonymous();

app.MapDelete("/api/v1/feed/{postId:guid}", async (Guid postId, HttpContext context, IAntiforgery antiforgery, IFeedService feed, CancellationToken cancellationToken) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(new { message = "La solicitud de seguridad ha caducado. Recarga la página." }); }

    var userIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdValue, out var authorId)) return Results.Unauthorized();
    return await feed.DeleteAsync(postId, authorId, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound(new { message = "La publicación no existe o no puedes eliminarla." });
}).RequireAuthorization();

app.MapGet("/api/v1/feed/media/{attachmentId:guid}", async (Guid attachmentId, IFeedService feed, CancellationToken cancellationToken) =>
{
    var attachment = await feed.OpenPublicAttachmentAsync(attachmentId, cancellationToken);
    return attachment is null
        ? Results.NotFound()
        : Results.File(attachment.Value.Content, attachment.Value.ContentType, attachment.Value.FileName, enableRangeProcessing: attachment.Value.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));
}).AllowAnonymous();

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var roles = new[] { "SocioAficion", "Jugador", "CuerpoTecnico", "Administrador" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded) throw new InvalidOperationException($"No se pudo crear el rol inicial {role}.");
        }
    }

    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminEmail = configuration["BootstrapAdmin:Email"];
    var adminPassword = configuration["BootstrapAdmin:Password"];
    var adminName = configuration["BootstrapAdmin:DisplayName"] ?? "Admin Buñol";
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) return;

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ClubUser>>();
    var normalizedEmail = adminEmail.Trim();
    var admin = await userManager.FindByEmailAsync(normalizedEmail);
    if (admin is null)
    {
        admin = new ClubUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            DisplayName = adminName,
            FirstName = adminName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? adminName,
            LastName = string.Join(' ', adminName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)),
            IsActive = true,
            LockoutEnabled = true
        };
        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            var safeErrors = string.Join(", ", createResult.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"No se pudo aprovisionar la cuenta Administrador inicial. Revisa la política de contraseña configurada. Códigos: {safeErrors}");
        }
    }
    else if (!admin.IsActive)
    {
        admin.IsActive = true;
        var updateResult = await userManager.UpdateAsync(admin);
        if (!updateResult.Succeeded) throw new InvalidOperationException("No se pudo activar la cuenta Administrador inicial.");
    }
    else if (!admin.LockoutEnabled)
    {
        admin.LockoutEnabled = true;
        var updateResult = await userManager.UpdateAsync(admin);
        if (!updateResult.Succeeded) throw new InvalidOperationException("No se pudo actualizar la seguridad de la cuenta Administrador inicial.");
    }

    if (!await userManager.IsInRoleAsync(admin, "Administrador"))
    {
        var roleResult = await userManager.AddToRoleAsync(admin, "Administrador");
        if (!roleResult.Succeeded) throw new InvalidOperationException("No se pudo asignar el rol Administrador inicial.");
    }
}

public partial class Program { }

public partial class Program;
