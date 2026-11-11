using BalonmanoBunol.Application.Social;
using BalonmanoBunol.Application.Auth;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BalonmanoBunol.Infrastructure.Persistence;

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
    if (context.Request.Path.StartsWithSegments("/api/v1/auth") || context.Request.Path.StartsWithSegments("/api/v1/admin"))
        context.Response.Headers.CacheControl = "no-store, no-cache";

    var isAuthMutation = HttpMethods.IsPost(context.Request.Method)
        && (context.Request.Path.StartsWithSegments("/api/v1/auth/register")
            || context.Request.Path.StartsWithSegments("/api/v1/auth/login")
            || context.Request.Path.StartsWithSegments("/api/v1/auth/logout")
            || context.Request.Path.StartsWithSegments("/api/v1/admin/role-requests"));

    if (isAuthMutation)
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

var roleRequests = app.MapGroup("/api/v1/admin/role-requests").RequireAuthorization(policy => policy.RequireRole("Administrador"));
roleRequests.MapGet("/", async (IAuthService auth, CancellationToken cancellationToken) =>
{
    var requests = await auth.GetRoleRequestsAsync(cancellationToken);
    return Results.Ok(requests.Select(request => new
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
    Results.Ok(await feed.GetLatestAsync(pageSize ?? 20, before, beforeId, cancellationToken)))
    .WithName("GetFeed");

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

public partial class Program;
