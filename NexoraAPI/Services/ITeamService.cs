using NexoraAPI.DTOs.Teams;

namespace NexoraAPI.Services;

public interface ITeamService
{
    Task<TeamResponseDto?> GetTeamByIdAsync(string id);
    Task<List<TeamResponseDto>> GetMyTeamsAsync(string userId);
    Task<TeamResponseDto> CreateTeamAsync(string userId, CreateTeamDto dto);
    Task<(bool Success, string Error)> AddMemberAsync(string teamId, string requestingUserId, AddMemberDto dto);
    Task<(bool Success, string Error)> RemoveMemberAsync(string teamId, string requestingUserId, string targetUserId);
    Task<(bool Success, string Error)> DeleteTeamAsync(string teamId, string requestingUserId);
}
