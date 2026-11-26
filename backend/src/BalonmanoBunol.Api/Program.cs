using BalonmanoBunol.Infrastructure;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var renderPort = builder.Configuration["PORT"];
if (int.TryParse(renderPort, out var port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("ClubDatabase")))
{
    throw new InvalidOperationException("Falta ConnectionStrings__ClubDatabase. Inicia la API con npm run api para cargar la configuración local desde .env.");
}
NeonConnectionStringValidator.EnsureDirectConnection(builder.Configuration.GetConnectionString("ClubDatabase"));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressReadingTokenFromFormBody = true;
    options.Cookie.Name = "cb-bunol-csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 401L * 1024 * 1024;
    options.ValueLengthLimit = 16 * 1024;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 }) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

await using (var startupScope = app.Services.CreateAsyncScope())
{
    await InitializeDatabaseAsync(startupScope.ServiceProvider);
}

var webRoot = builder.Configuration["Frontend:WebRoot"];
if (!string.IsNullOrWhiteSpace(webRoot))
{
    var fileProvider = new PhysicalFileProvider(Path.GetFullPath(webRoot));
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        var indexPath = Path.Combine(Path.GetFullPath(webRoot), "index.html");
        var hasSpaEntry = File.Exists(indexPath);
        var isSpaRoute = path.Equals("/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/feed", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/perfil", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/equipo", StringComparison.OrdinalIgnoreCase);
        if (isSpaRoute && hasSpaEntry)
            context.Response.Headers.CacheControl = "no-store, no-cache";
        await next();
    });
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        OnPrepareResponse = context =>
        {
            if (context.File.Name.Equals("sw.js", StringComparison.OrdinalIgnoreCase)
                || context.File.Name.Equals("manifest.webmanifest", StringComparison.OrdinalIgnoreCase))
                context.Context.Response.Headers.CacheControl = "no-cache";
        }
    });
}
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/auth")
        || context.Request.Path.StartsWithSegments("/api/v1/admin")
        || context.Request.Path.StartsWithSegments("/api/v1/profile")
        || context.Request.Path.StartsWithSegments("/api/v1/teams")
        || context.Request.Path.StartsWithSegments("/api/v1/my-teams"))
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

    var isProtectedMutation = (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
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
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "La solicitud de seguridad ha caducado. Recarga la página e inténtalo de nuevo." });
            return;
        }
    }

    await next();
});

app.MapControllers();
if (!string.IsNullOrWhiteSpace(webRoot))
{
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") || Path.HasExtension(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var indexPath = Path.Combine(Path.GetFullPath(webRoot), "index.html");
        if (!File.Exists(indexPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
    });
}
app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var dbContext = services.GetRequiredService<ClubDbContext>();
    await dbContext.Database.OpenConnectionAsync();
    var connection = dbContext.Database.GetDbConnection();
    try
    {
        await using (var acquireLock = connection.CreateCommand())
        {
            acquireLock.CommandText = "SELECT pg_advisory_lock(731942605182)";
            await acquireLock.ExecuteScalarAsync();
        }

        await dbContext.Database.MigrateAsync();
        await SeedIdentityAsync(services, configuration, dbContext);
    }
    finally
    {
        try
        {
            await using var releaseLock = connection.CreateCommand();
            releaseLock.CommandText = "SELECT pg_advisory_unlock(731942605182)";
            await releaseLock.ExecuteScalarAsync();
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}

static async Task SeedIdentityAsync(IServiceProvider services, IConfiguration configuration, ClubDbContext dbContext)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var roles = new[] { "SocioAficion", "Jugador", "CuerpoTecnico", "Administrador" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded) throw new InvalidOperationException($"No se pudo crear el rol inicial {role}.");
        }
    }

    var adminEmail = configuration["BootstrapAdmin:Email"];
    var adminPassword = configuration["BootstrapAdmin:Password"];
    var adminName = configuration["BootstrapAdmin:DisplayName"] ?? "Admin Buñol";
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword)) return;

    var userManager = services.GetRequiredService<UserManager<ClubUser>>();
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
