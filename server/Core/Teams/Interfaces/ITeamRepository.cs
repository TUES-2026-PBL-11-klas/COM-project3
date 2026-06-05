namespace Core.Teams;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Team>> GetByUserIdAsync(string userId, CancellationToken ct);
    Task<Team> CreateAsync(string name, string? description, string createdBy, CancellationToken ct);
    Task<bool> AddMemberAsync(string teamId, TeamMember member, CancellationToken ct);
    Task<bool> RemoveMemberAsync(string teamId, string userId, CancellationToken ct);
    Task<bool> DeleteAsync(string teamId, CancellationToken ct);
}
