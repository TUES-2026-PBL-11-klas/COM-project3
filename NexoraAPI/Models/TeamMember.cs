using MongoDB.Bson.Serialization.Attributes;

namespace NexoraAPI.Models;

public class TeamMember
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = TeamRole.Member;

    [BsonElement("joinedAt")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public static class TeamRole
{
    public const string Owner = "owner";
    public const string Member = "member";
}
