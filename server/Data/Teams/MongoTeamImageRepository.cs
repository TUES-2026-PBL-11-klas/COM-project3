using Core.Teams;
using Data.CleanMap;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Data.Teams;

public sealed class MongoTeamImageRepository : ITeamImageRepository
{
    private readonly IMongoCollection<TeamImageDocument> _images;

    public MongoTeamImageRepository(IOptions<CleanMapMongoOptions> options)
    {
        var settings = options.Value;
        var url = MongoUrl.Create(settings.ConnectionString);
        var mongoSettings = MongoClientSettings.FromUrl(url);
        mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

        var client = new MongoClient(mongoSettings);
        var database = client.GetDatabase(settings.DatabaseName);
        _images = database.GetCollection<TeamImageDocument>("team_images");

        var teamIdIndex = Builders<TeamImageDocument>.IndexKeys.Ascending(i => i.TeamId);
        _images.Indexes.CreateOne(new CreateIndexModel<TeamImageDocument>(teamIdIndex));
    }

    public async Task<IReadOnlyList<TeamImage>> GetByTeamIdAsync(string teamId, CancellationToken ct)
    {
        var docs = await _images
            .Find(i => i.TeamId == teamId)
            .SortByDescending(i => i.UploadedAt)
            .ToListAsync(ct);
        return docs.Select(d => d.ToDomain()).ToList();
    }

    public async Task<TeamImage> CreateAsync(
        string teamId, string uploadedBy, string imageUrl, string? notes, CancellationToken ct)
    {
        var doc = TeamMapper.ToDocument(teamId, uploadedBy, imageUrl, notes);
        await _images.InsertOneAsync(doc, cancellationToken: ct);
        return doc.ToDomain();
    }
}
