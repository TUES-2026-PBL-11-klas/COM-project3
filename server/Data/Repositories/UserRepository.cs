using MongoDB.Bson;
using MongoDB.Driver;
using Data.Entities;

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
        var filter = Builders<User>.Filter.Eq(u => u.Email, email.ToLowerInvariant());
        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByIdAsync(ObjectId id)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLowerInvariant();
        user.CreatedAt = DateTime.UtcNow;
        await _context.Users.InsertOneAsync(user);
        return user;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email.ToLowerInvariant());
        return await _context.Users.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        return await _context.Users.Find(filter).AnyAsync();
    }
}
