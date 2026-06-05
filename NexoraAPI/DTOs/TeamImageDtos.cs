namespace NexoraAPI.DTOs.Teams;

public record TeamImageResponseDto(
    string Id,
    string TeamId,
    string UploadedBy,
    string ImageUrl,
    string? Notes,
    DateTime UploadedAt
);
