namespace Core.Teams;

public interface ITeamService
{
    Task<Team?> GetTeamByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Team>> GetMyTeamsAsync(string userId, CancellationToken ct);
    Task<Team> CreateTeamAsync(string userId, CreateTeamRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> AddMemberAsync(string teamId, string requestingUserId, AddMemberRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> RemoveMemberAsync(string teamId, string requestingUserId, string targetUserId, CancellationToken ct);
    Task<(bool Success, string? Error)> DeleteTeamAsync(string teamId, string requestingUserId, CancellationToken ct);
}
