using NexoraAPI.Models;

namespace NexoraAPI.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id);
    Task<List<Team>> GetByUserIdAsync(string userId);
    Task CreateAsync(Team team);
    Task<bool> AddMemberAsync(string teamId, TeamMember member);
    Task<bool> RemoveMemberAsync(string teamId, string userId);
    Task<bool> DeleteAsync(string id);
}
