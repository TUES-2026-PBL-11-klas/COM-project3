using Core.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.Teams;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamsMongo(this IServiceCollection services)
    {
        // Re-uses CleanMapMongoOptions (same Atlas cluster, same database)
        services.AddSingleton<ITeamRepository, MongoTeamRepository>();
        services.AddSingleton<ITeamImageRepository, MongoTeamImageRepository>();
        return services;
    }
}
