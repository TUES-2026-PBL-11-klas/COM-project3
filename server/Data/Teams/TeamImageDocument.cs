using MongoDB.Bson.Serialization.Attributes;

namespace Data.Teams;

internal sealed class TeamImageDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    [BsonElement("teamId")]
    public string TeamId { get; init; } = string.Empty;

    [BsonElement("uploadedBy")]
    public string UploadedBy { get; init; } = string.Empty;

    [BsonElement("imageUrl")]
    public string ImageUrl { get; init; } = string.Empty;

    [BsonElement("notes")]
    public string? Notes { get; init; }

    [BsonElement("uploadedAt")]
    public long UploadedAt { get; init; }
}
