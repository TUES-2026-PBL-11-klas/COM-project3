using Core.CleanMap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data.CleanMap;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCleanMapMongo(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CleanMapMongoOptions>(options =>
        {
            configuration.GetSection(CleanMapMongoOptions.SectionName).Bind(options);

            var mongoSection = configuration.GetSection("MongoDB");
            var fallbackConnection =
                mongoSection["ConnectionString"] ?? configuration["MONGODB_CONNECTION_STRING"];
            var fallbackDatabase =
                mongoSection["DatabaseName"] ?? configuration["MONGODB_DATABASE_NAME"];
            var fallbackReports = mongoSection["ReportsCollectionName"];

            if ((string.IsNullOrWhiteSpace(options.ConnectionString)
                 || options.ConnectionString == "mongodb://localhost:27017")
                && !string.IsNullOrWhiteSpace(fallbackConnection))
            {
                options.ConnectionString = fallbackConnection;
            }

            if (string.IsNullOrWhiteSpace(options.DatabaseName)
                && !string.IsNullOrWhiteSpace(fallbackDatabase))
            {
                options.DatabaseName = fallbackDatabase;
            }

            if (string.IsNullOrWhiteSpace(options.ReportsCollectionName)
                && !string.IsNullOrWhiteSpace(fallbackReports))
            {
                options.ReportsCollectionName = fallbackReports;
            }
        });
        services.AddSingleton<MongoCleanMapReportRepository>();
        services.AddSingleton<ICleanMapReportRepository>(provider =>
            provider.GetRequiredService<MongoCleanMapReportRepository>());
        services.AddSingleton<ICleanMapHealthCheck>(provider =>
            provider.GetRequiredService<MongoCleanMapReportRepository>());

        return services;
    }
}
