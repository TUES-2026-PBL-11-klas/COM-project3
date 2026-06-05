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

        // GET /api/teams/my?userId=xxx
        group.MapGet("/my", async (string userId, ITeamService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest(new { error = "userId is required." });

            var teams = await service.GetMyTeamsAsync(userId, ct);
            return Results.Ok(teams.Select(ToResponse));
        });

        // GET /api/teams/{id}
        group.MapGet("/{id}", async (string id, ITeamService service, CancellationToken ct) =>
        {
            var team = await service.GetTeamByIdAsync(id, ct);
            return team is null ? Results.NotFound() : Results.Ok(ToResponse(team));
        });

        // POST /api/teams?userId=xxx
        group.MapPost("/", async (CreateTeamRequest request, string userId, ITeamService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest(new { error = "userId is required." });

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Team name is required." });

            var team = await service.CreateTeamAsync(userId, request, ct);
            return Results.Created($"/api/teams/{team.Id}", ToResponse(team));
        });

        // POST /api/teams/{id}/members?userId=xxx
        group.MapPost("/{id}/members", async (string id, AddMemberRequest request, string userId, ITeamService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest(new { error = "userId is required." });

            var (success, error) = await service.AddMemberAsync(id, userId, request, ct);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        // DELETE /api/teams/{id}/members/{targetUserId}?userId=xxx
        group.MapDelete("/{id}/members/{targetUserId}", async (string id, string targetUserId, string userId, ITeamService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest(new { error = "userId is required." });

            var (success, error) = await service.RemoveMemberAsync(id, userId, targetUserId, ct);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        // DELETE /api/teams/{id}?userId=xxx  (owner only)
        group.MapDelete("/{id}", async (string id, string userId, ITeamService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest(new { error = "userId is required." });

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
