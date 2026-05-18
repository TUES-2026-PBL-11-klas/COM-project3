using NexoraAPI.DTOs.Teams;
using NexoraAPI.Services;

namespace NexoraAPI.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Teams");

        // GET /api/teams/my?userId=xxx
        group.MapGet("/my", async (string userId, ITeamService service) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest("userId is required.");

            var teams = await service.GetMyTeamsAsync(userId);
            return Results.Ok(teams);
        });

        // GET /api/teams/{id}
        group.MapGet("/{id}", async (string id, ITeamService service) =>
        {
            var team = await service.GetTeamByIdAsync(id);
            return team is null ? Results.NotFound() : Results.Ok(team);
        });

        // POST /api/teams?userId=xxx
        group.MapPost("/", async (CreateTeamDto dto, string userId, ITeamService service) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest("userId is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Team name is required.");

            var team = await service.CreateTeamAsync(userId, dto);
            return Results.Created($"/api/teams/{team.Id}", team);
        });

        // POST /api/teams/{id}/members?userId=xxx
        group.MapPost("/{id}/members", async (string id, AddMemberDto dto, string userId, ITeamService service) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest("userId is required.");

            var (success, error) = await service.AddMemberAsync(id, userId, dto);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        // DELETE /api/teams/{id}/members/{targetUserId}?userId=xxx
        group.MapDelete("/{id}/members/{targetUserId}", async (string id, string targetUserId, string userId, ITeamService service) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest("userId is required.");

            var (success, error) = await service.RemoveMemberAsync(id, userId, targetUserId);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        // GET /api/teams/{id}/images
        group.MapGet("/{id}/images", async (string id, ITeamImageService imageService) =>
        {
            var images = await imageService.GetTeamImagesAsync(id);
            return Results.Ok(images);
        });

        // POST /api/teams/{id}/images?userId=xxx
        group.MapPost("/{id}/images", async (
            string id,
            string userId,
            IFormFile file,
            ITeamImageService imageService,
            string? notes = null) =>
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Results.BadRequest("userId is required.");

            var (success, error, image) = await imageService.UploadImageAsync(id, userId, file, notes);
            return success ? Results.Created($"/api/teams/{id}/images", image) : Results.BadRequest(new { error });
        }).DisableAntiforgery();
    }
}
