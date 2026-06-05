using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Data.Entities;

public class RefreshToken
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("userId")]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// SHA-256 hash of the actual token. We never store raw tokens.
    /// </summary>
    [BsonElement("tokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isRevoked")]
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// If this token was rotated, stores the hash of the replacement token.
    /// Used to detect reuse of old tokens (potential theft).
    /// </summary>
    [BsonElement("replacedByTokenHash")]
    public string? ReplacedByTokenHash { get; set; }
}
