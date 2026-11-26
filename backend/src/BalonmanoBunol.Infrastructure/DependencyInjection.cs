using BalonmanoBunol.Application.Auth;
using BalonmanoBunol.Infrastructure.Auth;
using BalonmanoBunol.Infrastructure.Administration;
using BalonmanoBunol.Infrastructure.Identity;
using BalonmanoBunol.Infrastructure.Persistence;
using BalonmanoBunol.Infrastructure.Profile;
using BalonmanoBunol.Infrastructure.Social;
using BalonmanoBunol.Infrastructure.Teams;
using BalonmanoBunol.Application.Social;
using BalonmanoBunol.Application.Profile;
using BalonmanoBunol.Application.Administration;
using BalonmanoBunol.Application.Teams;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Amazon.S3;
using Amazon.Runtime;

namespace BalonmanoBunol.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClubDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Falta configurar ConnectionStrings__ClubDatabase mediante el entorno de ejecución.");
        }
        var secureCookiePolicy = string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        services.AddDbContext<ClubDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpContextAccessor();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "cb-bunol-session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = secureCookiePolicy;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(12);
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
        services.AddIdentityCore<ClubUser>()
            .AddSignInManager<SignInManager<ClubUser>>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ClubDbContext>();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = configuration.GetValue("Identity:RequiredLength", 12);
            options.Password.RequireDigit = configuration.GetValue("Identity:RequireDigit", false);
            options.Password.RequireUppercase = configuration.GetValue("Identity:RequireUppercase", true);
            options.Password.RequireLowercase = configuration.GetValue("Identity:RequireLowercase", true);
            options.Password.RequireNonAlphanumeric = configuration.GetValue("Identity:RequireNonAlphanumeric", true);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        });

        services.AddScoped<IPostRepository, PostRepository>();
        var r2Configuration = new R2ObjectStorageConfiguration(
            configuration["FileStorage:Provider"], configuration["R2:Endpoint"], configuration["R2:Bucket"],
            configuration["R2:AccessKeyId"], configuration["R2:SecretAccessKey"]);
        r2Configuration.Validate();
        if (r2Configuration.IsEnabled)
        {
            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                new BasicAWSCredentials(r2Configuration.AccessKeyId, r2Configuration.SecretAccessKey),
                new AmazonS3Config { ServiceURL = r2Configuration.Endpoint, ForcePathStyle = true, AuthenticationRegion = "auto" }));
            services.AddSingleton<IPostMediaStorage>(serviceProvider => new R2PostMediaStorage(new R2ObjectStorage(
                serviceProvider.GetRequiredService<IAmazonS3>(), r2Configuration.Bucket!, "club-bunol/feed/")));
            services.AddSingleton<IProfileAvatarStorage>(serviceProvider => new R2ProfileAvatarStorage(new R2ObjectStorage(
                serviceProvider.GetRequiredService<IAmazonS3>(), r2Configuration.Bucket!, "club-bunol/avatars/")));
            services.AddSingleton<ITeamBoardStorage>(serviceProvider => new R2TeamBoardStorage(new R2ObjectStorage(
                serviceProvider.GetRequiredService<IAmazonS3>(), r2Configuration.Bucket!, "club-bunol/team-board/")));
        }
        else
        {
            services.AddSingleton<IPostMediaStorage>(serviceProvider =>
            {
                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                var rootPath = configuration["PostMedia:RootPath"];
                return new LocalPostMediaStorage(string.IsNullOrWhiteSpace(rootPath)
                    ? Path.Combine(environment.ContentRootPath, "App_Data", "post-media")
                    : rootPath);
            });
            services.AddSingleton<IProfileAvatarStorage>(serviceProvider =>
            {
                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                var rootPath = configuration["ProfileAvatar:RootPath"];
                return new LocalProfileAvatarStorage(string.IsNullOrWhiteSpace(rootPath)
                    ? Path.Combine(environment.ContentRootPath, "App_Data", "profile-avatars")
                    : rootPath);
            });
            services.AddSingleton<ITeamBoardStorage>(serviceProvider =>
            {
                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                var rootPath = configuration["TeamBoard:RootPath"];
                return new LocalTeamBoardStorage(string.IsNullOrWhiteSpace(rootPath)
                    ? Path.Combine(environment.ContentRootPath, "App_Data", "team-board")
                    : rootPath);
            });
        }
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAdministrationService, AdministrationService>();
        services.AddScoped<ITeamSpaceService, TeamSpaceService>();
        return services;
    }
}
