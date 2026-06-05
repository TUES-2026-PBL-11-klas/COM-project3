using MongoDB.Driver;
using Data.Entities;

namespace Data;

/// <summary>
/// Provides typed access to MongoDB collections.
/// Registers indexes on initialization for query performance.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        EnsureIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<RefreshToken> RefreshTokens => _database.GetCollection<RefreshToken>("refreshTokens");

    private void EnsureIndexes()
    {
        // Unique index on email for fast lookup and uniqueness constraint
        Users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));

        // Unique index on username
        Users.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Username),
            new CreateIndexOptions { Unique = true }));

        // Index on token hash for fast refresh-token lookups
        RefreshTokens.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.TokenHash)));

        // Index on userId for revoking all sessions
        RefreshTokens.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.UserId)));

        // TTL index to auto-delete expired tokens after 1 day past expiry
        RefreshTokens.Indexes.CreateOne(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(1) }));
    }
}
