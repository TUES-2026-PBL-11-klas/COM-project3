using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Core.Entities;
using Core.Repositories;

namespace Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MongoDbContext _context;

    public UserRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email.Trim().ToLowerInvariant());
        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByIdAsync(ObjectId id)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var pattern = new BsonRegularExpression($"^{Regex.Escape(username)}$", "i");
        var filter = Builders<User>.Filter.Regex(u => u.Username, pattern);
        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.Trim().ToLowerInvariant();
        user.CreatedAt = DateTime.UtcNow;
        await _context.Users.InsertOneAsync(user);
        return user;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email.Trim().ToLowerInvariant());
        return await _context.Users.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        var pattern = new BsonRegularExpression($"^{Regex.Escape(username)}$", "i");
        var filter = Builders<User>.Filter.Regex(u => u.Username, pattern);
        return await _context.Users.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return false;

        var filter = Builders<User>.Filter.Eq(u => u.Id, objectId);
        return await _context.Users.Find(filter).AnyAsync();
    }
}
