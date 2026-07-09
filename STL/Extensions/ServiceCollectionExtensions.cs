using System.Security.Claims;
using System.Text;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sieve.Models;
using Sieve.Services;
using STL.DbContexts;
using STL.Endpoints.ProductEndpoints;
using STL.Models.Auth;
using STL.MapperProfiles;
using STL.Infrastructure.Interceptors;
using STL.Infrastructure.Models.Settings;
using STL.SharedServices.Auth;
using STL.SharedServices.Caching;
using STL.SharedServices.File;
using STL.SharedServices.Recommendations;
using STL.SharedServices.UserContext;
using STL.Sieve;

namespace STL.WebApis.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddSingleton<TrackingInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("AppDbContext")
                ?? throw new InvalidOperationException(
                    "Connection string 'AppDbContext' is not configured.");

            options.UseNpgsql(connectionString);
            options.AddInterceptors(
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<TrackingInterceptor>());
        });

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection(nameof(JwtSettings)));

        services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<JwtSettings>>().Value);

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        var jwtSettings = configuration
            .GetSection(nameof(JwtSettings))
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings is not configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SigningKey))
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthConstants.Policies.User,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasMinimumRoleLevel(context.User, UserRoleLevel.User)));

            options.AddPolicy(
                AuthConstants.Policies.Manager,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasMinimumRoleLevel(context.User, UserRoleLevel.Manager)));

            options.AddPolicy(
                AuthConstants.Policies.Admin,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context =>
                        HasMinimumRoleLevel(context.User, UserRoleLevel.Admin)));
        });

        return services;
    }

    private static bool HasMinimumRoleLevel(
        ClaimsPrincipal user,
        UserRoleLevel minimumRoleLevel)
    {
        var roleLevelClaim = user.FindFirst(AuthConstants.RoleLevelClaim);

        return roleLevelClaim is not null
            && int.TryParse(roleLevelClaim.Value, out var roleLevel)
            && roleLevel >= (int)minimumRoleLevel;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SieveOptions>(
            configuration.GetSection("Sieve"));
        services.AddScoped<ISieveProcessor, ApplicationSieveProcessor>();
        services.AddAutoMapper(
            _ => { },
            typeof(ProductProfile).Assembly);
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IFileHandler, ExcelFileHandler>();
        services.AddScoped<IFileHandler, CsvFileHandler>();
        services.AddScoped<IProductRecommendationService, ProductRecommendationService>();

        return services;
    }

    public static IServiceCollection AddCacheServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException(
                    "Connection string 'Redis' is not configured.");
            options.InstanceName = "stl:";
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }

    public static IServiceCollection AddFastEndpointsConfiguration(
        this IServiceCollection services)
    {
        services.AddFastEndpoints(options =>
        {
            options.DisableAutoDiscovery = true;
            options.Assemblies = [typeof(CreateProductEndpoint).Assembly];
        }).SwaggerDocument();

        return services;
    }
}
