using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NexoraAPI.Configuration;
using NexoraAPI.Models;

namespace NexoraAPI.Repositories;

public class TeamImageRepository : ITeamImageRepository
{
    private readonly IMongoCollection<TeamImage> _images;

    public TeamImageRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _images = database.GetCollection<TeamImage>("team_images");
    }

    public async Task<List<TeamImage>> GetByTeamIdAsync(string teamId) =>
        await _images.Find(i => i.TeamId == teamId)
                     .SortByDescending(i => i.UploadedAt)
                     .ToListAsync();

    public async Task CreateAsync(TeamImage image) =>
        await _images.InsertOneAsync(image);
}
