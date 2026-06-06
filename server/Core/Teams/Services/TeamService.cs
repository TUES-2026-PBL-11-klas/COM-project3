using Core.Repositories;

namespace Core.Teams.Services;

public sealed class TeamService : ITeamService
{
    private readonly ITeamRepository _repository;
    private readonly IUserRepository _userRepository;

    public TeamService(ITeamRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

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

        var targetUserId = request.UserId;
        if (!await _userRepository.ExistsByIdAsync(targetUserId))
        {
            var userByName = await _userRepository.GetByUsernameAsync(targetUserId);
            if (userByName is null)
                return (false, "User does not exist.");

            targetUserId = userByName.Id.ToString();
        }

        if (team.Members.Any(m => m.UserId == targetUserId))
            return (false, "User is already a member of this team.");

        var member = new TeamMember { UserId = targetUserId, Role = TeamRole.Member, JoinedAt = DateTime.UtcNow };
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
