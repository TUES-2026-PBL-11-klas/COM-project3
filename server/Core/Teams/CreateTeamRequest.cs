namespace Core.Teams;

public sealed class CreateTeamRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
