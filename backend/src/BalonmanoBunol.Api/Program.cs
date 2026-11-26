using BalonmanoBunol.Infrastructure;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("ClubDatabase")))
{
    throw new InvalidOperationException("Falta ConnectionStrings__ClubDatabase. Inicia la API con npm run api para cargar la configuración local desde .env.");
}

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

await InitializeDatabaseAsync(app);

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
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
