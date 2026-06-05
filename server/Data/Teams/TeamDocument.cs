using MongoDB.Bson.Serialization.Attributes;

namespace Data.Teams;

internal sealed class TeamDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; init; }

    [BsonElement("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [BsonElement("members")]
    public List<TeamMemberDocument> Members { get; init; } = [];

    [BsonElement("createdAt")]
    public long CreatedAt { get; init; }

    [BsonElement("updatedAt")]
    public long UpdatedAt { get; init; }
}

internal sealed class TeamMemberDocument
{
    [BsonElement("userId")]
    public string UserId { get; init; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; init; } = string.Empty;

    [BsonElement("joinedAt")]
    public long JoinedAt { get; init; }
}
