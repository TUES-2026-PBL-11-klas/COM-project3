using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Core.Configuration;
using Core.Services;
using Data;
using Data.Repositories;
using API.Endpoints;
using API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

var mongoSettings = new MongoDbSettings();
builder.Configuration.GetSection(MongoDbSettings.SectionName).Bind(mongoSettings);
builder.Services.AddSingleton(mongoSettings);

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<MongoDbSettings>();
    return new MongoDbContext(settings.ConnectionString, settings.DatabaseName);
});

// ---------------------------------------------------------------------------
// Repositories
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService>(sp =>
{
    var settings = sp.GetRequiredService<JwtSettings>();
    return new TokenService(settings);
});
builder.Services.AddScoped<IAuthService, AuthService>();

// ---------------------------------------------------------------------------
// Authentication — JWT Bearer
// ---------------------------------------------------------------------------
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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.FromSeconds(30) // tight clock skew for short-lived tokens
    };
});

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// CORS — allow credentials for HttpOnly refresh cookie
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("cleanmap", policy =>
    {
        policy.WithOrigins("http://localhost:8000", "http://127.0.0.1:8000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ---------------------------------------------------------------------------
// OpenAPI
// ---------------------------------------------------------------------------
builder.Services.AddOpenApi();

// Keep the existing CleanMap JSON store for backwards compatibility
var dbPath = builder.Configuration["DbPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "cleanmap-db.json");
builder.Services.AddSingleton(new CleanMapStore(dbPath));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("cleanmap");

// CSRF middleware — must come after CORS but before auth/routing
app.UseMiddleware<CsrfMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------
// Auth endpoints
// ---------------------------------------------------------------------------
app.MapAuthEndpoints();

// ---------------------------------------------------------------------------
// CleanMap report endpoints (existing) — GET is public, POST requires auth
// ---------------------------------------------------------------------------
app.MapGet("/api/cleanmap/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/cleanmap/reports", (CleanMapStore store) => Results.Ok(store.GetAll()));

app.MapPost("/api/cleanmap/reports", (CleanMapReportCreate input, CleanMapStore store) =>
{
    var report = CleanMapReport.FromCreate(input);
    store.Add(report);
    return Results.Created($"/api/cleanmap/reports/{report.Id}", report);
}).RequireAuthorization();

app.MapPost("/api/cleanmap/reports/{id}/clean", (string id, CleanMapCleanRequest input, CleanMapStore store) =>
{
    var report = store.MarkClean(id, input);
    return report is null ? Results.NotFound() : Results.Ok(report);
}).RequireAuthorization();

app.Run();

// ===========================================================================
// Existing CleanMap types (unchanged)
// ===========================================================================
sealed class CleanMapStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly object _lock = new();
    private readonly string _path;
    private List<CleanMapReport> _reports = new();

    public CleanMapStore(string path)
    {
        _path = path;
        Load();
    }

    public List<CleanMapReport> GetAll()
    {
        lock (_lock)
        {
            return _reports.Select(report => report).ToList();
        }
    }

    public void Add(CleanMapReport report)
    {
        lock (_lock)
        {
            _reports.Add(report);
            Save();
        }
    }

    public CleanMapReport? MarkClean(string id, CleanMapCleanRequest input)
    {
        lock (_lock)
        {
            var report = _reports.FirstOrDefault(item => item.Id == id);
            if (report is null) return null;

            report.Status = "cleaned";
            report.PhotoAfter = input.PhotoAfter;
            report.CleanedAt = input.CleanedAt ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Save();
            return report;
        }
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        var json = File.ReadAllText(_path);
        var data = JsonSerializer.Deserialize<List<CleanMapReport>>(json, JsonOptions);
        if (data is not null)
        {
            _reports = data;
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_reports, JsonOptions);
        File.WriteAllText(_path, json);
    }
}

sealed class CleanMapReport
{
    public string Id { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Address { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Notes { get; set; }
    public string Status { get; set; } = "dirty";
    public string? PhotoBefore { get; set; }
    public string? PhotoAfter { get; set; }
    public long CreatedAt { get; set; }
    public long? CleanedAt { get; set; }

    public static CleanMapReport FromCreate(CleanMapReportCreate input)
    {
        var id = string.IsNullOrWhiteSpace(input.Id)
            ? $"rep_{Guid.NewGuid():N}".Substring(0, 12)
            : input.Id;
        var createdAt = input.CreatedAt > 0
            ? input.CreatedAt
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return new CleanMapReport
        {
            Id = id,
            Lat = input.Lat,
            Lng = input.Lng,
            Address = input.Address,
            Tags = input.Tags ?? new List<string>(),
            Notes = input.Notes,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "dirty" : input.Status,
            PhotoBefore = input.PhotoBefore,
            PhotoAfter = input.PhotoAfter,
            CreatedAt = createdAt,
            CleanedAt = input.CleanedAt
        };
    }
}

sealed class CleanMapReportCreate
{
    public string? Id { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string? Address { get; set; }
    public List<string>? Tags { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; }
    public string? PhotoBefore { get; set; }
    public string? PhotoAfter { get; set; }
    public long CreatedAt { get; set; }
    public long? CleanedAt { get; set; }
}

sealed class CleanMapCleanRequest
{
    public string? PhotoAfter { get; set; }
    public long? CleanedAt { get; set; }
}
