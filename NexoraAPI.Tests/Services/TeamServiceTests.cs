using Moq;
using NexoraAPI.DTOs.Teams;
using NexoraAPI.Models;
using NexoraAPI.Repositories;
using NexoraAPI.Services;

namespace NexoraAPI.Tests.Services;

public class TeamServiceTests
{
    private readonly Mock<ITeamRepository> _repoMock;
    private readonly TeamService _service;

    public TeamServiceTests()
    {
        _repoMock = new Mock<ITeamRepository>();
        _service = new TeamService(_repoMock.Object);
    }

    // ─── CreateTeamAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTeamAsync_ShouldCreateTeam_WithOwnerAsMember()
    {
        // Arrange
        var userId = "user-1";
        var dto = new CreateTeamDto("Green Warriors", "We clean parks");
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Team>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTeamAsync(userId, dto);

        // Assert
        Assert.Equal("Green Warriors", result.Name);
        Assert.Equal(userId, result.CreatedBy);
        Assert.Single(result.Members);
        Assert.Equal(TeamRole.Owner, result.Members[0].Role);
        Assert.Equal(userId, result.Members[0].UserId);
    }

    [Fact]
    public async Task CreateTeamAsync_ShouldTrimNameAndDescription()
    {
        // Arrange
        var dto = new CreateTeamDto("  Eco Team  ", "  Clean up  ");
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Team>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTeamAsync("user-1", dto);

        // Assert
        Assert.Equal("Eco Team", result.Name);
        Assert.Equal("Clean up", result.Description);
    }

    // ─── AddMemberAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddMemberAsync_ShouldFail_WhenTeamNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((Team?)null);

        // Act
        var (success, error) = await _service.AddMemberAsync("nonexistent", "owner-1", new AddMemberDto("new-user"));

        // Assert
        Assert.False(success);
        Assert.Equal("Team not found.", error);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldFail_WhenRequesterIsNotOwner()
    {
        // Arrange
        var team = BuildTeam("team-1", "owner-1", extraMember: "member-1");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);

        // Act — member-1 tries to add someone
        var (success, error) = await _service.AddMemberAsync("team-1", "member-1", new AddMemberDto("new-user"));

        // Assert
        Assert.False(success);
        Assert.Equal("Only the team owner can add members.", error);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldFail_WhenUserAlreadyMember()
    {
        // Arrange
        var team = BuildTeam("team-1", "owner-1", extraMember: "existing-user");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);

        // Act
        var (success, error) = await _service.AddMemberAsync("team-1", "owner-1", new AddMemberDto("existing-user"));

        // Assert
        Assert.False(success);
        Assert.Equal("User is already a member of this team.", error);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldSucceed_WhenOwnerAddsNewUser()
    {
        // Arrange
        var team = BuildTeam("team-1", "owner-1");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);
        _repoMock.Setup(r => r.AddMemberAsync("team-1", It.IsAny<TeamMember>())).ReturnsAsync(true);

        // Act
        var (success, error) = await _service.AddMemberAsync("team-1", "owner-1", new AddMemberDto("new-user"));

        // Assert
        Assert.True(success);
        Assert.Empty(error);
    }

    // ─── RemoveMemberAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveMemberAsync_ShouldFail_WhenOwnerTriesToRemoveThemselves()
    {
        // Arrange
        var team = BuildTeam("team-1", "owner-1", extraMember: "member-1");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);

        // Act
        var (success, error) = await _service.RemoveMemberAsync("team-1", "owner-1", "owner-1");

        // Assert
        Assert.False(success);
        Assert.Equal("Owner cannot remove themselves from the team.", error);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldFail_WhenTargetNotInTeam()
    {
        // Arrange
        var team = BuildTeam("team-1", "owner-1");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);

        // Act
        var (success, error) = await _service.RemoveMemberAsync("team-1", "owner-1", "ghost-user");

        // Assert
        Assert.False(success);
        Assert.Equal("User is not a member of this team.", error);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldSucceed_WhenOwnerRemovesMember()
    {
        // Arrange
        var team = BuildTeam("team-1", "owner-1", extraMember: "member-1");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);
        _repoMock.Setup(r => r.RemoveMemberAsync("team-1", "member-1")).ReturnsAsync(true);

        // Act
        var (success, error) = await _service.RemoveMemberAsync("team-1", "owner-1", "member-1");

        // Assert
        Assert.True(success);
        Assert.Empty(error);
    }

    // ─── DeleteTeamAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTeamAsync_ShouldFail_WhenTeamNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((Team?)null);

        var (success, error) = await _service.DeleteTeamAsync("nonexistent", "owner-1");

        Assert.False(success);
        Assert.Equal("Team not found.", error);
    }

    [Fact]
    public async Task DeleteTeamAsync_ShouldFail_WhenRequesterIsNotOwner()
    {
        var team = BuildTeam("team-1", "owner-1", extraMember: "member-1");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);

        var (success, error) = await _service.DeleteTeamAsync("team-1", "member-1");

        Assert.False(success);
        Assert.Equal("Only the team owner can delete the team.", error);
    }

    [Fact]
    public async Task DeleteTeamAsync_ShouldSucceed_WhenOwnerDeletes()
    {
        var team = BuildTeam("team-1", "owner-1");
        _repoMock.Setup(r => r.GetByIdAsync("team-1")).ReturnsAsync(team);
        _repoMock.Setup(r => r.DeleteAsync("team-1")).ReturnsAsync(true);

        var (success, error) = await _service.DeleteTeamAsync("team-1", "owner-1");

        Assert.True(success);
        Assert.Empty(error);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────

    private static Team BuildTeam(string id, string ownerId, string? extraMember = null)
    {
        var members = new List<TeamMember>
        {
            new() { UserId = ownerId, Role = TeamRole.Owner }
        };

        if (extraMember is not null)
            members.Add(new() { UserId = extraMember, Role = TeamRole.Member });

        return new Team
        {
            Id = id,
            Name = "Test Team",
            CreatedBy = ownerId,
            Members = members
        };
    }
}
