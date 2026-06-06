using Core.Repositories;
using Core.Teams;
using Core.Users;

namespace Tests;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users;

    public FakeUserRepository(IEnumerable<User>? users = null)
    {
        _users = users?.Select(CopyUser).ToList() ?? new List<User>();
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = _users.FirstOrDefault(u => string.Equals(u.Email.Trim().ToLowerInvariant(), normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(string id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User> CreateAsync(User user)
    {
        var copy = CopyUser(user);
        if (string.IsNullOrWhiteSpace(copy.Id))
        {
            copy.Id = Guid.NewGuid().ToString();
        }

        _users.Add(copy);
        return Task.FromResult(copy);
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var exists = _users.Any(u => string.Equals(u.Email.Trim().ToLowerInvariant(), normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByUsernameAsync(string username)
    {
        var exists = _users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsByIdAsync(string id)
    {
        var exists = _users.Any(u => u.Id == id);
        return Task.FromResult(exists);
    }

    private static User CopyUser(User user)
    {
        return new User
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
            Points = user.Points,
            CreatedAt = user.CreatedAt
        };
    }
}

public sealed class FakeTeamRepository : ITeamRepository
{
    private readonly Dictionary<string, Team> _teams;

    public FakeTeamRepository(IEnumerable<Team>? teams = null)
    {
        _teams = teams?.ToDictionary(t => t.Id, t => CopyTeam(t)) ?? new Dictionary<string, Team>();
    }

    public Task<Team?> GetByIdAsync(string id, CancellationToken ct)
    {
        _teams.TryGetValue(id, out var team);
        return Task.FromResult(team);
    }

    public Task<IReadOnlyList<Team>> GetByUserIdAsync(string userId, CancellationToken ct)
    {
        var teams = _teams.Values.Where(t => t.Members.Any(m => m.UserId == userId)).ToList().AsReadOnly();
        return Task.FromResult((IReadOnlyList<Team>)teams);
    }

    public Task<Team> CreateAsync(string name, string? description, string createdBy, CancellationToken ct)
    {
        var team = new Team
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            CreatedBy = createdBy,
            Members = new List<TeamMember>
            {
                new() { UserId = createdBy, Role = TeamRole.Owner, JoinedAt = DateTime.UtcNow }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _teams.Add(team.Id, team);
        return Task.FromResult(team);
    }

    public Task<bool> AddMemberAsync(string teamId, TeamMember member, CancellationToken ct)
    {
        if (!_teams.TryGetValue(teamId, out var team))
            return Task.FromResult(false);

        if (team.Members.Any(m => m.UserId == member.UserId))
            return Task.FromResult(false);

        var updatedMembers = team.Members.Append(member).ToList().AsReadOnly();
        _teams[teamId] = new Team
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedBy = team.CreatedBy,
            Members = updatedMembers,
            CreatedAt = team.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult(true);
    }

    public Task<bool> RemoveMemberAsync(string teamId, string userId, CancellationToken ct)
    {
        if (!_teams.TryGetValue(teamId, out var team))
            return Task.FromResult(false);

        if (team.Members.All(m => m.UserId != userId))
            return Task.FromResult(false);

        var updatedMembers = team.Members.Where(m => m.UserId != userId).ToList().AsReadOnly();
        _teams[teamId] = new Team
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedBy = team.CreatedBy,
            Members = updatedMembers,
            CreatedAt = team.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string teamId, CancellationToken ct)
    {
        return Task.FromResult(_teams.Remove(teamId));
    }

    private static Team CopyTeam(Team team)
    {
        return new Team
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedBy = team.CreatedBy,
            Members = team.Members.ToList().AsReadOnly(),
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt
        };
    }
}
