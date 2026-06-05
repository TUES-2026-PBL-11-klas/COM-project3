using Core.CleanMap;
using Core.Teams;
using Core.Teams.Services;
using Data.CleanMap;
using Data.Teams;
using System.Text;
using Core.Configuration;
using Core.Services;
using Data;
using Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCleanMapApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddCleanMapMongo(configuration);
        services.AddTeamsMongo();
        services.AddCleanMapAuth(configuration);
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

    private static IServiceCollection AddCleanMapAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
        services.AddSingleton(jwtSettings);

        var mongoSettings = new MongoDbSettings();
        configuration.GetSection(MongoDbSettings.SectionName).Bind(mongoSettings);
        services.AddSingleton(mongoSettings);

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<MongoDbSettings>();
            return new MongoDbContext(settings.ConnectionString, settings.DatabaseName);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService>(sp =>
        {
            var settings = sp.GetRequiredService<JwtSettings>();
            return new TokenService(settings);
        });
        services.AddScoped<IAuthService, AuthService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        services.AddAuthorization();
        
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
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }
}
