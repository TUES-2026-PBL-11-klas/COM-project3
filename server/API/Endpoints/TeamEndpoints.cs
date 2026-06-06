using System.Security.Claims;
using Core.Teams;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Endpoints;

public static class TeamEndpoints
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static void MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/teams");

        group.MapGet("/my", [Microsoft.AspNetCore.Authorization.Authorize] async (
            HttpContext httpContext,
            ITeamService service,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var teams = await service.GetMyTeamsAsync(userId, ct);
            return Results.Ok(teams.Select(ToResponse));
        });

        group.MapGet("/{id}", async (string id, ITeamService service, CancellationToken ct) =>
        {
            var team = await service.GetTeamByIdAsync(id, ct);
            return team is null ? Results.NotFound() : Results.Ok(ToResponse(team));
        });

        group.MapPost("/", [Microsoft.AspNetCore.Authorization.Authorize] async (
            HttpContext httpContext,
            CreateTeamRequest request,
            ITeamService service,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Team name is required." });

            var team = await service.CreateTeamAsync(userId, request, ct);
            return Results.Created($"/api/teams/{team.Id}", ToResponse(team));
        });

        group.MapPost("/{id}/members", [Microsoft.AspNetCore.Authorization.Authorize] async (
            string id,
            HttpContext httpContext,
            AddMemberRequest request,
            ITeamService service,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var (success, error) = await service.AddMemberAsync(id, userId, request, ct);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        group.MapDelete("/{id}/members/{targetUserId}", [Microsoft.AspNetCore.Authorization.Authorize] async (
            string id,
            string targetUserId,
            HttpContext httpContext,
            ITeamService service,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var (success, error) = await service.RemoveMemberAsync(id, userId, targetUserId, ct);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        group.MapDelete("/{id}", [Microsoft.AspNetCore.Authorization.Authorize] async (
            string id,
            HttpContext httpContext,
            ITeamService service,
            CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (string.IsNullOrWhiteSpace(userId))
                return Results.Unauthorized();

            var (success, error) = await service.DeleteTeamAsync(id, userId, ct);
            return success ? Results.NoContent() : Results.BadRequest(new { error });
        });

        // GET /api/teams/{id}/images
        group.MapGet("/{id}/images", async (string id, ITeamImageService imageService, CancellationToken ct) =>
        {
            var images = await imageService.GetTeamImagesAsync(id, ct);
            return Results.Ok(images.Select(ToImageResponse));
        });

        // POST /api/teams/{id}/images?userId=xxx&notes=xxx
        group.MapPost("/{id}/images", async (
            string id,
            string userId,
            IFormFile file,
            ITeamImageService imageService,
            IWebHostEnvironment env,
            CancellationToken ct,
            string? notes = null) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest(new { error = "userId is required." });

            if (file.Length == 0)
                return Results.BadRequest(new { error = "File is empty." });

            if (file.Length > MaxFileSizeBytes)
                return Results.BadRequest(new { error = "File exceeds 5 MB limit." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return Results.BadRequest(new { error = "Only JPG, PNG and WebP files are allowed." });

            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var uploadsDir = Path.Combine(webRoot, "uploads", "teams", id);
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream, ct);
            }

            var imageUrl = $"/uploads/teams/{id}/{fileName}";
            var (success, error, image) = await imageService.SaveImageAsync(id, userId, imageUrl, notes, ct);

            if (!success)
            {
                // Clean up file if service rejected
                if (File.Exists(filePath)) File.Delete(filePath);
                return Results.BadRequest(new { error });
            }

            return Results.Created($"/api/teams/{id}/images", ToImageResponse(image!));
        }).DisableAntiforgery();
    }

    private static string? GetUserId(HttpContext httpContext)
    {
        var user = httpContext.User;
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("sub");
    }

    private static object ToResponse(Team team) => new
    {
        id = team.Id,
        name = team.Name,
        description = team.Description,
        createdBy = team.CreatedBy,
        members = team.Members.Select(m => new
        {
            userId = m.UserId,
            role = m.Role,
            joinedAt = m.JoinedAt,
        }),
        createdAt = team.CreatedAt,
    };

    private static object ToImageResponse(TeamImage img) => new
    {
        id = img.Id,
        teamId = img.TeamId,
        uploadedBy = img.UploadedBy,
        imageUrl = img.ImageUrl,
        notes = img.Notes,
        uploadedAt = img.UploadedAt,
    };
}
