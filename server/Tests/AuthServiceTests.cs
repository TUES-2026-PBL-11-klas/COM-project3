using Core.Configuration;
using Core.DTOs;
using Core.Services;
using Core.Entities;
using MongoDB.Bson;
using Xunit;

namespace Tests;

public sealed class AuthServiceTests
{
    private readonly JwtSettings _jwtSettings = new()
    {
        Issuer = "cleanmap-api",
        Audience = "cleanmap-users",
        SecretKey = "test-super-secret-key-at-least-32-characters",
        AccessTokenExpiryMinutes = 60
    };

    private readonly IPasswordService _passwordService = new PasswordService();
    private readonly ITokenService _tokenService;

    public AuthServiceTests() => _tokenService = new TokenService(_jwtSettings);

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenEmailIsInvalid()
    {
        var repo = new FakeUserRepository();
        var service = new AuthService(repo, new FakeRefreshTokenRepository(), _passwordService, _tokenService, _jwtSettings);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "not-an-email",
            Password = "Password1",
            Username = "user123"
        });

        Assert.Null(response);
        Assert.Equal("Invalid email format.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsError_WhenPasswordIsWeak()
    {
        var repo = new FakeUserRepository();
        var service = new AuthService(repo, new FakeRefreshTokenRepository(), _passwordService, _tokenService, _jwtSettings);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "user@example.com",
            Password = "weak",
            Username = "user123"
        });

        Assert.Null(response);
        Assert.Equal("Password must be at least 8 characters long.", error);
    }

    [Fact]
    public async Task SignupAsync_ReturnsAuthResponse_WhenValid()
    {
        var repo = new FakeUserRepository();
        var service = new AuthService(repo, new FakeRefreshTokenRepository(), _passwordService, _tokenService, _jwtSettings);

        var (response, error) = await service.SignupAsync(new SignupRequest
        {
            Email = "user@example.com",
            Password = "Password1",
            Username = "user123"
        });

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.NotNull(response!.AccessToken);
        Assert.Equal("user123", response.User.Username);
        Assert.Equal("user@example.com", response.User.Email);
    }

    [Fact]
    public async Task LoginAsync_ReturnsError_WhenPasswordIsInvalid()
    {
        var repo = new FakeUserRepository();
        var createdUser = new User
        {
            Id = ObjectId.GenerateNewId(),
            Username = "user123",
            Email = "user@example.com",
            PasswordHash = _passwordService.Hash("Password1"),
            Role = "citizen"
        };

        await repo.CreateAsync(createdUser);
        var service = new AuthService(repo, new FakeRefreshTokenRepository(), _passwordService, _tokenService, _jwtSettings);

        var (response, refreshToken, error) = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrong-password"
        });

        Assert.Null(response);
        Assert.Equal("Invalid email or password.", error);
    }

    [Fact]
    public async Task LoginAsync_ReturnsAuthResponse_WhenValid()
    {
        var repo = new FakeUserRepository();
        var createdUser = new User
        {
            Id = ObjectId.GenerateNewId(),
            Username = "user123",
            Email = "user@example.com",
            PasswordHash = _passwordService.Hash("Password1"),
            Role = "citizen"
        };

        await repo.CreateAsync(createdUser);
        var service = new AuthService(repo, new FakeRefreshTokenRepository(), _passwordService, _tokenService, _jwtSettings);

        var (response, refreshToken, error) = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password1"
        });

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.Equal("user123", response!.User.Username);
        Assert.Equal("user@example.com", response.User.Email);
    }
}
