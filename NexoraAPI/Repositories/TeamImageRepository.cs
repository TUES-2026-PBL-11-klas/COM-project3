using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NexoraAPI.Configuration;
using NexoraAPI.Models;

namespace NexoraAPI.Repositories;

public class TeamImageRepository : MongoRepositoryBase<TeamImage>, ITeamImageRepository
{
    private readonly IMongoCollection<TeamImage> _images;

    public TeamImageRepository(IOptions<MongoDbSettings> settings) : base(settings, "team_images")
    {
        _images = Collection;

        // БД: Index on teamId for efficient GetByTeamIdAsync queries
        var teamIdIndex = Builders<TeamImage>.IndexKeys.Ascending(i => i.TeamId);
        _images.Indexes.CreateOne(new CreateIndexModel<TeamImage>(teamIdIndex));
    }

    public async Task<List<TeamImage>> GetByTeamIdAsync(string teamId) =>
        await _images.Find(i => i.TeamId == teamId)
                     .SortByDescending(i => i.UploadedAt)
                     .ToListAsync();

    public async Task CreateAsync(TeamImage image) =>
        await _images.InsertOneAsync(image);
}
