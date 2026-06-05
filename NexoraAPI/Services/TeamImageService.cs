using NexoraAPI.DTOs.Teams;
using NexoraAPI.Models;
using NexoraAPI.Repositories;

namespace NexoraAPI.Services;

public class TeamImageService : ITeamImageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ITeamImageRepository _imageRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IWebHostEnvironment _env;

    public TeamImageService(
        ITeamImageRepository imageRepository,
        ITeamRepository teamRepository,
        IWebHostEnvironment env)
    {
        _imageRepository = imageRepository;
        _teamRepository = teamRepository;
        _env = env;
    }

    public async Task<List<TeamImageResponseDto>> GetTeamImagesAsync(string teamId)
    {
        var images = await _imageRepository.GetByTeamIdAsync(teamId);
        return images.Select(MapToDto).ToList();
    }

    public async Task<(bool Success, string Error, TeamImageResponseDto? Image)> UploadImageAsync(
        string teamId, string userId, IFormFile file, string? notes)
    {
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team is null)
            return (false, "Team not found.", null);

        if (team.Members.All(m => m.UserId != userId))
            return (false, "You are not a member of this team.", null);

        if (file.Length == 0)
            return (false, "File is empty.", null);

        if (file.Length > MaxFileSizeBytes)
            return (false, "File exceeds 5 MB limit.", null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (false, "Only JPG, PNG and WebP files are allowed.", null);

        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "teams", teamId);
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        var imageUrl = $"/uploads/teams/{teamId}/{fileName}";

        var teamImage = new TeamImage
        {
            TeamId = teamId,
            UploadedBy = userId,
            ImageUrl = imageUrl,
            Notes = notes?.Trim()
        };

        await _imageRepository.CreateAsync(teamImage);
        return (true, string.Empty, MapToDto(teamImage));
    }

    private static TeamImageResponseDto MapToDto(TeamImage img) => new(
        img.Id,
        img.TeamId,
        img.UploadedBy,
        img.ImageUrl,
        img.Notes,
        img.UploadedAt
    );
}
