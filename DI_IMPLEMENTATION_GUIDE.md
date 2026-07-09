# STL DI Implementation Guide

Muc tieu cua file nay: giup ban tu code lai phan DI trong STL, khong chi nhin code da sua san.

DI trong STL co 2 viec khac nhau:

```text
builder.Services... = dang ky nhung thu app co the dung
app.Use...          = sap xep request di qua nhung lop nao truoc khi vao endpoint
```

Vi du voi auth:

```csharp
builder.Services.AddAuthentication(...);
builder.Services.AddAuthorization();
```

la dang ky he thong auth vao container.

Con:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

la bat auth trong request pipeline.

Neu chi co dang ky ma khong `Use...`, request se khong duoc parse token. Neu chi `Use...` ma khong dang ky, app khong biet auth la gi.

## 1. Mental model

Kien truc hien tai cua STL gan voi cach chia cua Core1:

```text
STL.WebApis
  Program.cs
  Extensions/
    ServiceCollectionExtensions.cs
    ApplicationBuilderExtensions.cs

STL.Domain
  Endpoints/
  Entities/
  DbContexts/
  Migrations/
  MapperProfiles/
  SharedServices/
  Models/

STL.Redis
  RedisCacheService
```

Vai tro:

```text
WebAPI  = composition root, noi lap rap dependency.
Domain  = noi chua endpoint, entity, dbcontext, mapper, service dung chung.
Libs    = adapter ben ngoai, vi du Redis.
```

Noi ngan gon: WebAPI khong can chua nghiep vu, nhung WebAPI la noi dang ky dependency de app chay duoc.

## 2. Vi sao tach DI ra extension method?

Neu de het trong `Program.cs`, file se dai va kho doc:

```csharp
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddAuthentication(...);
builder.Services.AddAuthorization();
builder.Services.AddAutoMapper(...);
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddStackExchangeRedisCache(...);
builder.Services.AddFastEndpoints(...);
```

Nen tach thanh cac nhom:

```csharp
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCacheServices(builder.Configuration);
builder.Services.AddFastEndpointsConfiguration();
```

Luc nay `Program.cs` giong muc luc. Chi tiet nam trong `ServiceCollectionExtensions.cs`.

## 3. Tao file ServiceCollectionExtensions

Tao file:

```text
STL/STL/Extensions/ServiceCollectionExtensions.cs
```

Khung co ban:

```csharp
namespace STL.WebApis.Extensions;

public static class ServiceCollectionExtensions
{
}
```

Luu y:

- Class phai la `static`.
- Method extension phai co `this IServiceCollection services`.
- Nen return lai `services` de goi tiep duoc.

## 4. Dang ky database

Muc tieu: cho phep endpoint/service inject `AppDbContext`.

```csharp
public static IServiceCollection AddDatabaseServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<AppDbContext>(options =>
    {
        var connectionString = configuration.GetConnectionString("AppDbContext")
            ?? throw new InvalidOperationException(
                "Connection string 'AppDbContext' is not configured.");

        options.UseNpgsql(connectionString);
    });

    return services;
}
```

Can co config trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AppDbContext": "Host=localhost;Port=5432;Database=stl;Username=stl;Password=stl"
  }
}
```

Sau khi dang ky, endpoint co the dung:

```csharp
public class SomeEndpoint(AppDbContext context) : EndpointWithoutRequest
{
}
```

## 5. Dang ky authentication va authorization

Muc tieu:

- App doc duoc JWT tu request.
- Endpoint co `[Authorize]` thi duoc bao ve.
- Service co the generate token.
- Code co the doc user hien tai qua `IUserContext`.

```csharp
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

    services.AddAuthorization();

    return services;
}
```

Y nghia tung dong quan trong:

```text
Configure<JwtSettings>        = bind config JwtSettings tu appsettings.
AddHttpContextAccessor        = cho service doc duoc HttpContext hien tai.
IUserContext/UserContext      = boc thong tin user hien tai thanh service gon hon.
IJwtTokenService/JwtToken...  = service tao access token, refresh token.
AddAuthentication             = dang ky co che xac thuc.
AddJwtBearer                  = noi app cach validate JWT.
AddAuthorization              = bat he thong phan quyen.
```

Config can co:

```json
{
  "JwtSettings": {
    "Issuer": "STL",
    "Audience": "STL.Client",
    "SigningKey": "your-long-secret-key"
  }
}
```

## 6. Dang ky application services

Muc tieu:

- Dang ky Sieve cho paging/filter/sort.
- Dang ky AutoMapper de map entity/DTO.

```csharp
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

    return services;
}
```

Dong quan trong nhat:

```csharp
typeof(ProductProfile).Assembly
```

Nghia la: AutoMapper hay scan assembly dang chua `ProductProfile`.

Vi mapper nam trong Domain, WebAPI phai chi ro assembly cua Domain. Neu khong, AutoMapper co the khong tim thay profile.

## 7. Dang ky cache/Redis

Muc tieu:

- Dang ky Redis distributed cache.
- Khi code can `ICacheService`, DI se dua `RedisCacheService`.

```csharp
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
```

Config:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  },
  "Cache": {
    "AbsoluteExpirationMinutes": 10
  }
}
```

Viec dang ky:

```csharp
services.AddSingleton<ICacheService, RedisCacheService>();
```

co nghia la:

```text
Ai can ICacheService thi dua RedisCacheService.
```

## 8. Dang ky FastEndpoints

Muc tieu: WebAPI scan endpoint nam trong Domain.

```csharp
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
```

Vi sao can dong nay?

```csharp
options.Assemblies = [typeof(CreateProductEndpoint).Assembly];
```

Vi endpoint khong nam trong WebAPI assembly nua. No nam trong Domain. Neu khong khai bao assembly, FastEndpoints co the khong tim thay endpoint.

## 9. Program.cs sau khi gom DI

File:

```text
STL/STL/Program.cs
```

Nen gon nhu sau:

```csharp
using STL.WebApis.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCacheServices(builder.Configuration);
builder.Services.AddFastEndpointsConfiguration();

var app = builder.Build();

app.UseApplicationPipeline();

app.Run();
```

Neu doc `Program.cs`, ban phai tra loi duoc app dang co nhung nhom nao:

```text
Database
Authentication
Application services
Cache
FastEndpoints
Pipeline
```

## 10. Tao ApplicationBuilderExtensions

Tao file:

```text
STL/STL/Extensions/ApplicationBuilderExtensions.cs
```

No quan ly request pipeline:

```csharp
namespace STL.WebApis.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();

        app.UseSwaggerGen();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapFastEndpoints();

        return app;
    }
}
```

Thu tu quan trong:

```text
UseAuthentication truoc UseAuthorization.
MapFastEndpoints sau auth.
```

Giai thich:

```text
UseAuthentication = doc token, tao HttpContext.User.
UseAuthorization  = dua vao User va policy de cho/chan request.
MapFastEndpoints  = request vao endpoint that.
```

## 11. Khi nao can middleware/pre-processor rieng?

JWT middleware chi tra loi cau hoi:

```text
Token co hop le khong?
User la ai?
```

Neu can rule sau thi moi can middleware/pre-processor rieng:

```text
User nay co duoc truy cap resource nay khong?
Tenant trong request co khop tenant cua user khong?
Product/Category nay co thuoc pham vi user khong?
Feature nay co duoc bat cho user/tenant khong?
```

Core1 co `SecurityProcessor` vi no can rule sau JWT, vi du kiem tra tenant/powerPlant.

STL hien tai chua can middleware phuc tap neu chi co Product/Category co ban. Khi them ownership, role, permission, tenant thi luc do hay tao processor.

## 12. Checklist tu code lai DI

Lam theo dung thu tu nay:

1. Tao `ServiceCollectionExtensions.cs`.
2. Viet `AddDatabaseServices`.
3. Them connection string Postgres vao `appsettings.json`.
4. Viet `AddFastEndpointsConfiguration`.
5. Dam bao endpoint nam trong assembly duoc scan.
6. Viet `AddApplicationServices` cho AutoMapper/Sieve.
7. Viet `AddAuthenticationServices` cho JWT.
8. Viet `AddCacheServices` cho Redis.
9. Tao `ApplicationBuilderExtensions.cs`.
10. Trong `Program.cs`, goi cac extension theo tung nhom.
11. Build solution.
12. Chay migration/update database neu co thay doi entity.

## 13. Loi hay gap

### Endpoint khong hien trong Swagger

Kiem tra:

```csharp
options.Assemblies = [typeof(CreateProductEndpoint).Assembly];
```

Neu endpoint nam trong Domain ma WebAPI chi scan assembly WebAPI, Swagger se khong thay endpoint.

### Mapper khong map duoc

Kiem tra:

```csharp
services.AddAutoMapper(
    _ => { },
    typeof(ProductProfile).Assembly);
```

Neu profile nam trong Domain, phai scan Domain assembly.

### Inject AppDbContext bi loi

Kiem tra:

```csharp
services.AddDbContext<AppDbContext>(...);
```

va connection string:

```json
"AppDbContext": "Host=localhost;Port=5432;Database=stl;Username=stl;Password=stl"
```

### Endpoint co Authorize nhung token khong duoc doc

Kiem tra pipeline:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapFastEndpoints();
```

### Redis inject duoc nhung khong connect

Kiem tra:

```json
"Redis": "localhost:6379,abortConnect=false"
```

va container Redis da chay chua.

## 14. Cau can nho

```text
WebAPI la noi lap rap.
Domain la noi chua code nghiep vu.
Libs la noi noi voi ben ngoai.
DI la ban ke khai: interface nao dung implementation nao, config nao lay tu dau.
Pipeline la duong di cua request truoc khi vao endpoint.
```

