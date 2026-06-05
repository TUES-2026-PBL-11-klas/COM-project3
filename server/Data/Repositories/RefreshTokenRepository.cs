using MongoDB.Bson;
using MongoDB.Driver;
using Data.Entities;

namespace Data.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly MongoDbContext _context;

    public RefreshTokenRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(RefreshToken token)
    {
        token.CreatedAt = DateTime.UtcNow;
        await _context.RefreshTokens.InsertOneAsync(token);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(rt => rt.TokenHash, tokenHash);
        return await _context.RefreshTokens.Find(filter).FirstOrDefaultAsync();
    }

    public async Task RevokeAsync(ObjectId tokenId, string? replacedByTokenHash = null)
    {
        var update = Builders<RefreshToken>.Update
            .Set(rt => rt.IsRevoked, true);

        if (replacedByTokenHash is not null)
        {
            update = update.Set(rt => rt.ReplacedByTokenHash, replacedByTokenHash);
        }

        var filter = Builders<RefreshToken>.Filter.Eq(rt => rt.Id, tokenId);
        await _context.RefreshTokens.UpdateOneAsync(filter, update);
    }

    public async Task RevokeAllForUserAsync(ObjectId userId)
    {
        var filter = Builders<RefreshToken>.Filter.Eq(rt => rt.UserId, userId)
                   & Builders<RefreshToken>.Filter.Eq(rt => rt.IsRevoked, false);

        var update = Builders<RefreshToken>.Update.Set(rt => rt.IsRevoked, true);

        await _context.RefreshTokens.UpdateManyAsync(filter, update);
    }
}
