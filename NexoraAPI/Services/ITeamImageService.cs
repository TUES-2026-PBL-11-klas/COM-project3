using NexoraAPI.DTOs.Teams;

namespace NexoraAPI.Services;

public interface ITeamImageService
{
    Task<List<TeamImageResponseDto>> GetTeamImagesAsync(string teamId);
    Task<(bool Success, string Error, TeamImageResponseDto? Image)> UploadImageAsync(
        string teamId, string userId, IFormFile file, string? notes);
}
