namespace NexoraAPI.DTOs.Teams;

public record CreateTeamDto(string Name, string? Description);

public record AddMemberDto(string UserId);

public record TeamMemberDto(string UserId, string Role, DateTime JoinedAt);

public record TeamResponseDto(
    string Id,
    string Name,
    string? Description,
    string CreatedBy,
    List<TeamMemberDto> Members,
    DateTime CreatedAt
);
