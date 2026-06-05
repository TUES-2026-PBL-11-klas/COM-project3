using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NexoraAPI.Configuration;
using System.Security.Authentication;

namespace NexoraAPI.Repositories;

/// <summary>
/// Abstract base class providing shared MongoDB connection setup for all repositories.
/// Demonstrates OOP inheritance and encapsulation — concrete repositories inherit
/// connection logic without duplicating TLS configuration.
/// </summary>
public abstract class MongoRepositoryBase<TDocument>
{
    protected readonly IMongoCollection<TDocument> Collection;

    protected MongoRepositoryBase(IOptions<MongoDbSettings> settings, string collectionName)
    {
        var clientSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
        clientSettings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12,
            CheckCertificateRevocation = false,
            ServerCertificateValidationCallback = (sender, cert, chain, errors) => true
        };
        clientSettings.AllowInsecureTls = true;

        var client = new MongoClient(clientSettings);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        Collection = database.GetCollection<TDocument>(collectionName);
    }
}
