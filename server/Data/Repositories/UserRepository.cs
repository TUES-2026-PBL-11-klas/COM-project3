using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Core.Repositories;
using DomainUser = Core.Users.User;
using PersistenceUser = Data.Entities.User;

namespace Data.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly MongoDbContext _context;

    public UserRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<DomainUser?> GetByEmailAsync(string email)
    {
        var filter = Builders<PersistenceUser>.Filter.Eq(u => u.Email, email.Trim().ToLowerInvariant());
        var document = await _context.Users.Find(filter).FirstOrDefaultAsync();
        return document is null ? null : ToDomain(document);
    }

    public async Task<DomainUser?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var filter = Builders<Data.Entities.User>.Filter.Eq(u => u.Id, objectId);
        var document = await _context.Users.Find(filter).FirstOrDefaultAsync();
        return document is null ? null : ToDomain(document);
    }

    public async Task<DomainUser> CreateAsync(DomainUser user)
    {
        var document = ToEntity(user);
        document.Email = document.Email.Trim().ToLowerInvariant();
        document.CreatedAt = DateTime.UtcNow;
        await _context.Users.InsertOneAsync(document);
        return ToDomain(document);
    }

    public async Task<DomainUser?> GetByUsernameAsync(string username)
    {
        var pattern = new BsonRegularExpression($"^{Regex.Escape(username)}$", "i");
        var filter = Builders<PersistenceUser>.Filter.Regex(u => u.Username, pattern);
        var document = await _context.Users.Find(filter).FirstOrDefaultAsync();
        return document is null ? null : ToDomain(document);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var filter = Builders<Data.Entities.User>.Filter.Eq(u => u.Email, email.Trim().ToLowerInvariant());
        return await _context.Users.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        var pattern = new BsonRegularExpression($"^{Regex.Escape(username)}$", "i");
        var filter = Builders<Data.Entities.User>.Filter.Regex(u => u.Username, pattern);
        return await _context.Users.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var filter = Builders<Data.Entities.User>.Filter.Eq(u => u.Id, objectId);
        return await _context.Users.Find(filter).AnyAsync();
    }

    private static DomainUser ToDomain(PersistenceUser entity)
    {
        return new DomainUser
        {
            Id = entity.Id.ToString(),
            Username = entity.Username,
            Email = entity.Email,
            PasswordHash = entity.PasswordHash,
            Role = entity.Role,
            Points = entity.Points,
            CreatedAt = entity.CreatedAt
        };
    }

    private static PersistenceUser ToEntity(DomainUser user)
    {
        var entity = new PersistenceUser
        {
            Username = user.Username,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            Points = user.Points,
            CreatedAt = user.CreatedAt
        };

        if (ObjectId.TryParse(user.Id, out var objectId))
        {
            entity.Id = objectId;
        }

        return entity;
    }
}
