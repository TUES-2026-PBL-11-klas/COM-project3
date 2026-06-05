namespace Core.Teams;

public sealed class TeamImage
{
    public string Id { get; init; } = string.Empty;
    public string TeamId { get; init; } = string.Empty;
    public string UploadedBy { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime UploadedAt { get; init; }
}
