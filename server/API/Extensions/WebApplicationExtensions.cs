using API.Endpoints;
using API.Middleware;

namespace API.Extensions;
public static class WebApplicationExtensions
{
    public static WebApplication UseCleanMapApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(wwwroot);
        app.UseStaticFiles();
        app.UseCors(CleanMapCorsPolicyNames.CleanMap);
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<CleanMapExceptionMiddleware>();
        app.UseMiddleware<CsrfMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapAuthEndpoints();
        app.MapCleanMapEndpoints();
        app.MapTeamEndpoints();

        return app;
    }
}
