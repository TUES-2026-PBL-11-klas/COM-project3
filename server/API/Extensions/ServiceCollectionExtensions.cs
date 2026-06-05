using Core.CleanMap;
using Core.Teams;
using Core.Teams.Services;
using Data.CleanMap;
using Data.Teams;

namespace API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCleanMapApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddCleanMapMongo(configuration);
        services.AddTeamsMongo();
        services.AddCleanMapServices();
        services.AddTeamsServices();
        services.AddCleanMapCors();

        return services;
    }

    private static IServiceCollection AddCleanMapServices(this IServiceCollection services)
    {
        services.AddScoped<ICleanMapReportService, CleanMapReportService>();
        return services;
    }

    private static IServiceCollection AddTeamsServices(this IServiceCollection services)
    {
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<ITeamImageService, TeamImageService>();
        return services;
    }

    private static IServiceCollection AddCleanMapCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CleanMapCorsPolicyNames.CleanMap, policy =>
            {
                policy.WithOrigins(
                        "http://localhost:8000",
                        "http://127.0.0.1:8000",
                        "http://localhost:5173",
                        "http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
