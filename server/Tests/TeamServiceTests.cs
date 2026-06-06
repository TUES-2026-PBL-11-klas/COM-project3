using Core.Services;
using Core.Teams;
using Core.Teams.Services;
using Core.Users;
using Xunit;

namespace Tests;

public sealed class TeamServiceTests
{
    [Fact]
    public async Task AddMemberAsync_ReturnsSuccess_WhenOwnerAddsExistingUserByUsername()
    {
        var ownerId = "owner-1";
        var userId = "user-123";
        var team = CreateTeam(ownerId);
        var userRepo = new FakeUserRepository(new[]
        {
            new User { Id = userId, Username = "123", Email = "123@example.com" }
        });
        var teamRepo = new FakeTeamRepository(new[] { team });
        var service = new TeamService(teamRepo, userRepo);

        var (success, error) = await service.AddMemberAsync(team.Id, ownerId, new AddMemberRequest { UserId = "123" }, CancellationToken.None);

        Assert.True(success);
        Assert.Null(error);

        var updatedTeam = await teamRepo.GetByIdAsync(team.Id, CancellationToken.None);
        Assert.NotNull(updatedTeam);
        Assert.Contains(updatedTeam!.Members, m => m.UserId == userId && m.Role == TeamRole.Member);
    }

    [Fact]
    public async Task AddMemberAsync_ReturnsError_WhenUserDoesNotExist()
    {
        var ownerId = "owner-2";
        var team = CreateTeam(ownerId);
        var userRepo = new FakeUserRepository();
        var teamRepo = new FakeTeamRepository(new[] { team });
        var service = new TeamService(teamRepo, userRepo);

        var (success, error) = await service.AddMemberAsync(team.Id, ownerId, new AddMemberRequest { UserId = "missing" }, CancellationToken.None);

        Assert.False(success);
        Assert.Equal("User does not exist.", error);
    }

    [Fact]
    public async Task AddMemberAsync_ReturnsError_WhenRequesterIsNotOwner()
    {
        var ownerId = "owner-3";
        var team = CreateTeam(ownerId);
        var userRepo = new FakeUserRepository(new[]
        {
            new User { Id = "user-456", Username = "456", Email = "456@example.com" }
        });
        var teamRepo = new FakeTeamRepository(new[] { team });
        var service = new TeamService(teamRepo, userRepo);

        var (success, error) = await service.AddMemberAsync(team.Id, "member-1", new AddMemberRequest { UserId = "456" }, CancellationToken.None);

        Assert.False(success);
        Assert.Equal("Only the team owner can add members.", error);
    }

    [Fact]
    public async Task RemoveMemberAsync_ReturnsSuccess_WhenOwnerRemovesMember()
    {
        var ownerId = "owner-4";
        var memberId = "member-4";
        var team = CreateTeam(ownerId, new[] { memberId });
        var userRepo = new FakeUserRepository();
        var teamRepo = new FakeTeamRepository(new[] { team });
        var service = new TeamService(teamRepo, userRepo);

        var (success, error) = await service.RemoveMemberAsync(team.Id, ownerId, memberId, CancellationToken.None);

        Assert.True(success);
        Assert.Null(error);

        var updatedTeam = await teamRepo.GetByIdAsync(team.Id, CancellationToken.None);
        Assert.NotNull(updatedTeam);
        Assert.DoesNotContain(updatedTeam!.Members, m => m.UserId == memberId);
    }

    [Fact]
    public async Task RemoveMemberAsync_ReturnsError_WhenOwnerTriesToRemoveSelf()
    {
        var ownerId = "owner-5";
        var team = CreateTeam(ownerId);
        var userRepo = new FakeUserRepository();
        var teamRepo = new FakeTeamRepository(new[] { team });
        var service = new TeamService(teamRepo, userRepo);

        var (success, error) = await service.RemoveMemberAsync(team.Id, ownerId, ownerId, CancellationToken.None);

        Assert.False(success);
        Assert.Equal("Owner cannot remove themselves from the team.", error);
    }

    private static Team CreateTeam(string ownerId, IEnumerable<string>? memberIds = null)
    {
        var members = new List<TeamMember>
        {
            new() { UserId = ownerId, Role = TeamRole.Owner, JoinedAt = DateTime.UtcNow }
        };

        if (memberIds is not null)
        {
            members.AddRange(memberIds.Select(id => new TeamMember { UserId = id, Role = TeamRole.Member, JoinedAt = DateTime.UtcNow }));
        }

        return new Team
        {
            Id = "team-1",
            Name = "Test Team",
            CreatedBy = ownerId,
            Members = members,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
