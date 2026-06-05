namespace Core.Teams;

public sealed class TeamMember
{
    public string UserId { get; init; } = string.Empty;
    public string Role { get; init; } = TeamRole.Member;
    public DateTime JoinedAt { get; init; }
}

public static class TeamRole
{
    public const string Owner = "owner";
    public const string Member = "member";
}
