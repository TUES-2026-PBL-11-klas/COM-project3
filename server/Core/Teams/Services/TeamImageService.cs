namespace Core.Teams.Services;

public sealed class TeamImageService : ITeamImageService
{
    private readonly ITeamRepository _teams;
    private readonly ITeamImageRepository _images;

    public TeamImageService(ITeamRepository teams, ITeamImageRepository images)
    {
        _teams = teams;
        _images = images;
    }

    public Task<IReadOnlyList<TeamImage>> GetTeamImagesAsync(string teamId, CancellationToken ct) =>
        _images.GetByTeamIdAsync(teamId, ct);

    public async Task<(bool Success, string? Error, TeamImage? Image)> SaveImageAsync(
        string teamId, string userId, string imageUrl, string? notes, CancellationToken ct)
    {
        var team = await _teams.GetByIdAsync(teamId, ct);
        if (team is null) return (false, "Team not found.", null);

        if (team.Members.All(m => m.UserId != userId))
            return (false, "You are not a member of this team.", null);

        var image = await _images.CreateAsync(teamId, userId, imageUrl, notes?.Trim(), ct);
        return (true, null, image);
    }
}
