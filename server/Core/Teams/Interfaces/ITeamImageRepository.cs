namespace Core.Teams;

public interface ITeamImageRepository
{
    Task<IReadOnlyList<TeamImage>> GetByTeamIdAsync(string teamId, CancellationToken ct);
    Task<TeamImage> CreateAsync(string teamId, string uploadedBy, string imageUrl, string? notes, CancellationToken ct);
}
