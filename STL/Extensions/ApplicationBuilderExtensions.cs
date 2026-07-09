using FastEndpoints;
using FastEndpoints.Swagger;

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
