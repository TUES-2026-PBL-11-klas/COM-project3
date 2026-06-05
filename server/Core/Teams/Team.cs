namespace Core.Teams;

public sealed class Team
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public IReadOnlyList<TeamMember> Members { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
