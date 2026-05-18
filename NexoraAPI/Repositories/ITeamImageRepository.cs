using NexoraAPI.Models;

namespace NexoraAPI.Repositories;

public interface ITeamImageRepository
{
    Task<List<TeamImage>> GetByTeamIdAsync(string teamId);
    Task CreateAsync(TeamImage image);
}
