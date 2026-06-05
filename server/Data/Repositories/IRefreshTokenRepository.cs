using MongoDB.Bson;
using Data.Entities;

namespace Data.Repositories;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken token);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task RevokeAsync(ObjectId tokenId, string? replacedByTokenHash = null);
    Task RevokeAllForUserAsync(ObjectId userId);
}
