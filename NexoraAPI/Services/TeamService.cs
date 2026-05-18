using NexoraAPI.DTOs.Teams;
using NexoraAPI.Models;
using NexoraAPI.Repositories;

namespace NexoraAPI.Services;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _repository;

    public TeamService(ITeamRepository repository)
    {
        _repository = repository;
    }

    public async Task<TeamResponseDto?> GetTeamByIdAsync(string id)
    {
        var team = await _repository.GetByIdAsync(id);
        return team is null ? null : MapToDto(team);
    }

    public async Task<List<TeamResponseDto>> GetMyTeamsAsync(string userId)
    {
        var teams = await _repository.GetByUserIdAsync(userId);
        return teams.Select(MapToDto).ToList();
    }

    public async Task<TeamResponseDto> CreateTeamAsync(string userId, CreateTeamDto dto)
    {
        var team = new Team
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CreatedBy = userId,
            Members = new List<TeamMember>
            {
                new() { UserId = userId, Role = TeamRole.Owner }
            }
        };

        await _repository.CreateAsync(team);
        return MapToDto(team);
    }

    public async Task<(bool Success, string Error)> AddMemberAsync(string teamId, string requestingUserId, AddMemberDto dto)
    {
        var team = await _repository.GetByIdAsync(teamId);
        if (team is null)
            return (false, "Team not found.");

        var requester = team.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester?.Role != TeamRole.Owner)
            return (false, "Only the team owner can add members.");

        if (team.Members.Any(m => m.UserId == dto.UserId))
            return (false, "User is already a member of this team.");

        var newMember = new TeamMember { UserId = dto.UserId, Role = TeamRole.Member };
        var success = await _repository.AddMemberAsync(teamId, newMember);
        return (success, success ? string.Empty : "Failed to add member.");
    }

    public async Task<(bool Success, string Error)> RemoveMemberAsync(string teamId, string requestingUserId, string targetUserId)
    {
        var team = await _repository.GetByIdAsync(teamId);
        if (team is null)
            return (false, "Team not found.");

        var requester = team.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester?.Role != TeamRole.Owner)
            return (false, "Only the team owner can remove members.");

        if (targetUserId == requestingUserId)
            return (false, "Owner cannot remove themselves from the team.");

        if (team.Members.All(m => m.UserId != targetUserId))
            return (false, "User is not a member of this team.");

        var success = await _repository.RemoveMemberAsync(teamId, targetUserId);
        return (success, success ? string.Empty : "Failed to remove member.");
    }

    private static TeamResponseDto MapToDto(Team team) => new(
        team.Id,
        team.Name,
        team.Description,
        team.CreatedBy,
        team.Members.Select(m => new TeamMemberDto(m.UserId, m.Role, m.JoinedAt)).ToList(),
        team.CreatedAt
    );
}
