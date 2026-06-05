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

        // Ensure wwwroot exists before serving static files (team image uploads)
        var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(wwwroot);
        app.UseStaticFiles();
        app.UseCors(CleanMapCorsPolicyNames.CleanMap);
        app.UseMiddleware<CleanMapExceptionMiddleware>();
        app.MapCleanMapEndpoints();
        app.MapTeamEndpoints();

        return app;
    }
}
