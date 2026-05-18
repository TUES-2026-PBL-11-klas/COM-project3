using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NexoraAPI.Models;

public class TeamImage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("teamId")]
    public string TeamId { get; set; } = string.Empty;

    [BsonElement("uploadedBy")]
    public string UploadedBy { get; set; } = string.Empty;

    [BsonElement("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
