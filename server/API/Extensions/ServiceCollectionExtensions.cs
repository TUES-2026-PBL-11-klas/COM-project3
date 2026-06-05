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
                var rawOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
                var origins = string.IsNullOrWhiteSpace(rawOrigins)
                    ? Array.Empty<string>()
                    : rawOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        return services;
    }
}
