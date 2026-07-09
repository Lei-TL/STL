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
