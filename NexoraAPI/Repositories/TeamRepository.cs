using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NexoraAPI.Configuration;
using NexoraAPI.Models;

namespace NexoraAPI.Repositories;

public class TeamRepository : MongoRepositoryBase<Team>, ITeamRepository
{
    private readonly IMongoCollection<Team> _teams;

    public TeamRepository(IOptions<MongoDbSettings> settings) : base(settings, "teams")
    {
        _teams = Collection;

        // БД: Compound index on members.userId for efficient GetByUserIdAsync queries
        var membersIndex = Builders<Team>.IndexKeys.Ascending("members.userId");
        _teams.Indexes.CreateOne(new CreateIndexModel<Team>(membersIndex));
    }

    public async Task<Team?> GetByIdAsync(string id) =>
        await _teams.Find(t => t.Id == id).FirstOrDefaultAsync();

    public async Task<List<Team>> GetByUserIdAsync(string userId) =>
        await _teams.Find(t => t.Members.Any(m => m.UserId == userId)).ToListAsync();

    public async Task CreateAsync(Team team) =>
        await _teams.InsertOneAsync(team);

    public async Task<bool> AddMemberAsync(string teamId, TeamMember member)
    {
        var update = Builders<Team>.Update
            .Push(t => t.Members, member)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);
        var result = await _teams.UpdateOneAsync(t => t.Id == teamId, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> RemoveMemberAsync(string teamId, string userId)
    {
        var update = Builders<Team>.Update
            .PullFilter(t => t.Members, m => m.UserId == userId)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);
        var result = await _teams.UpdateOneAsync(t => t.Id == teamId, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _teams.DeleteOneAsync(t => t.Id == id);
        return result.DeletedCount > 0;
    }
}
