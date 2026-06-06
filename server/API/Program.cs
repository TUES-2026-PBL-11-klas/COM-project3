using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Core.Configuration;
using Core.Repositories;
using Core.Services;
using Data;
using Data.Repositories;
using API.Extensions;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration["CleanMapDatabase:ConnectionString"]
        ?? builder.Configuration["MongoDB:ConnectionString"]
        ?? builder.Configuration["MONGODB_CONNECTION_STRING"]
        ?? "mongodb://localhost:27017";
    var databaseName = builder.Configuration["CleanMapDatabase:DatabaseName"]
        ?? builder.Configuration["MongoDB:DatabaseName"]
        ?? builder.Configuration["MONGODB_DATABASE_NAME"]
        ?? "nexora";

    return new MongoDbContext(connectionString, databaseName);
});

builder.Services.AddAuthentication(options =>
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

builder.Services.AddAuthorization();

builder.AddCleanMapObservability();
builder.Services.AddCleanMapApi(builder.Configuration);

var app = builder.Build();

app.UseCleanMapApi();

app.Run();
