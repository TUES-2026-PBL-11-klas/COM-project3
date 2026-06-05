namespace Core.Teams.Services;

public sealed class TeamService : ITeamService
{
    private readonly ITeamRepository _repository;

    public TeamService(ITeamRepository repository) => _repository = repository;

    public Task<Team?> GetTeamByIdAsync(string id, CancellationToken ct) =>
        _repository.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Team>> GetMyTeamsAsync(string userId, CancellationToken ct) =>
        _repository.GetByUserIdAsync(userId, ct);

    public async Task<Team> CreateTeamAsync(string userId, CreateTeamRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var description = request.Description?.Trim();
        return await _repository.CreateAsync(name, description, userId, ct);
    }

    public async Task<(bool Success, string? Error)> AddMemberAsync(
        string teamId, string requestingUserId, AddMemberRequest request, CancellationToken ct)
    {
        var team = await _repository.GetByIdAsync(teamId, ct);
        if (team is null) return (false, "Team not found.");

        var requester = team.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester?.Role != TeamRole.Owner) return (false, "Only the team owner can add members.");

        if (team.Members.Any(m => m.UserId == request.UserId))
            return (false, "User is already a member of this team.");

        var member = new TeamMember { UserId = request.UserId, Role = TeamRole.Member, JoinedAt = DateTime.UtcNow };
        var success = await _repository.AddMemberAsync(teamId, member, ct);
        return (success, success ? null : "Failed to add member.");
    }

    public async Task<(bool Success, string? Error)> RemoveMemberAsync(
        string teamId, string requestingUserId, string targetUserId, CancellationToken ct)
    {
        var team = await _repository.GetByIdAsync(teamId, ct);
        if (team is null) return (false, "Team not found.");

        var requester = team.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester?.Role != TeamRole.Owner) return (false, "Only the team owner can remove members.");

        if (targetUserId == requestingUserId) return (false, "Owner cannot remove themselves from the team.");

        if (team.Members.All(m => m.UserId != targetUserId)) return (false, "User is not a member of this team.");

        var success = await _repository.RemoveMemberAsync(teamId, targetUserId, ct);
        return (success, success ? null : "Failed to remove member.");
    }

    public async Task<(bool Success, string? Error)> DeleteTeamAsync(
        string teamId, string requestingUserId, CancellationToken ct)
    {
        var team = await _repository.GetByIdAsync(teamId, ct);
        if (team is null) return (false, "Team not found.");

        var requester = team.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester?.Role != TeamRole.Owner) return (false, "Only the team owner can delete the team.");

        var success = await _repository.DeleteAsync(teamId, ct);
        return (success, success ? null : "Failed to delete team.");
    }
}
