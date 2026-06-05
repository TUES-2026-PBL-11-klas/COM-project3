namespace Core.Teams;

public interface ITeamImageService
{
    Task<IReadOnlyList<TeamImage>> GetTeamImagesAsync(string teamId, CancellationToken ct);
    Task<(bool Success, string? Error, TeamImage? Image)> SaveImageAsync(
        string teamId, string userId, string imageUrl, string? notes, CancellationToken ct);
}
