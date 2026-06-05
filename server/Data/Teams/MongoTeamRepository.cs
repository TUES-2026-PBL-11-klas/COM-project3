using Core.Teams;
using Data.CleanMap;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Data.Teams;

public sealed class MongoTeamRepository : ITeamRepository
{
    private readonly IMongoCollection<TeamDocument> _teams;

    public MongoTeamRepository(IOptions<CleanMapMongoOptions> options)
    {
        var settings = options.Value;
        var url = MongoUrl.Create(settings.ConnectionString);
        var mongoSettings = MongoClientSettings.FromUrl(url);
        mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

        var client = new MongoClient(mongoSettings);
        var database = client.GetDatabase(settings.DatabaseName);
        _teams = database.GetCollection<TeamDocument>("teams");

        var membersIndex = Builders<TeamDocument>.IndexKeys.Ascending("members.userId");
        _teams.Indexes.CreateOne(new CreateIndexModel<TeamDocument>(membersIndex));
    }

    public async Task<Team?> GetByIdAsync(string id, CancellationToken ct)
    {
        var doc = await _teams.Find(t => t.Id == id).FirstOrDefaultAsync(ct);
        return doc?.ToDomain();
    }

    public async Task<IReadOnlyList<Team>> GetByUserIdAsync(string userId, CancellationToken ct)
    {
        var docs = await _teams
            .Find(t => t.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);
        return docs.Select(d => d.ToDomain()).ToList();
    }

    public async Task<Team> CreateAsync(string name, string? description, string createdBy, CancellationToken ct)
    {
        var doc = TeamMapper.ToDocument(name, description, createdBy);
        await _teams.InsertOneAsync(doc, cancellationToken: ct);
        return doc.ToDomain();
    }

    public async Task<bool> AddMemberAsync(string teamId, TeamMember member, CancellationToken ct)
    {
        var memberDoc = member.ToDocument();
        var update = Builders<TeamDocument>.Update
            .Push(t => t.Members, memberDoc)
            .Set(t => t.UpdatedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var result = await _teams.UpdateOneAsync(t => t.Id == teamId, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> RemoveMemberAsync(string teamId, string userId, CancellationToken ct)
    {
        var update = Builders<TeamDocument>.Update
            .PullFilter(t => t.Members, m => m.UserId == userId)
            .Set(t => t.UpdatedAt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var result = await _teams.UpdateOneAsync(t => t.Id == teamId, update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string teamId, CancellationToken ct)
    {
        var result = await _teams.DeleteOneAsync(t => t.Id == teamId, ct);
        return result.DeletedCount > 0;
    }
}
