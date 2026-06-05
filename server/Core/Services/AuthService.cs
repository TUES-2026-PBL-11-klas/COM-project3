using System.Text.RegularExpressions;
using Core.Configuration;
using Core.DTOs;
using Data.Entities;
using Data.Repositories;
using MongoDB.Bson;

namespace Core.Services;

public partial class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    // Simple email regex — covers 99% of valid emails without being overly strict
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public AuthService(
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IPasswordService passwordService,
        ITokenService tokenService,
        JwtSettings jwtSettings)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings;
    }

    public async Task<(AuthResponse? Response, string? Error)> SignupAsync(SignupRequest request)
    {
        // Validate email format
        if (!EmailRegex().IsMatch(request.Email))
            return (null, "Invalid email format.");

        // Validate password complexity
        var (isValid, error) = _passwordService.ValidateComplexity(request.Password);
        if (!isValid)
            return (null, error);

        // Validate username length
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return (null, "Username must be at least 3 characters.");

        if (request.Username.Length > 30)
            return (null, "Username must be at most 30 characters.");

        // Check uniqueness
        if (await _userRepo.ExistsByEmailAsync(request.Email))
            return (null, "An account with this email already exists.");

        if (await _userRepo.ExistsByUsernameAsync(request.Username))
            return (null, "This username is already taken.");

        // Create user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordService.Hash(request.Password),
            Role = "citizen"
        };

        await _userRepo.CreateAsync(user);

        // Generate access token (no refresh token on signup — user must login after signup)
        var accessToken = _tokenService.GenerateAccessToken(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            User = MapToDto(user)
        };

        return (response, null);
    }

    public async Task<(AuthResponse? Response, string? RefreshToken, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user is null)
            return (null, null, "Invalid email or password.");

        if (!_passwordService.Verify(request.Password, user.PasswordHash))
            return (null, null, "Invalid email or password.");

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(refreshToken);

        // Store refresh token hash in DB
        var tokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        };

        await _refreshTokenRepo.CreateAsync(tokenEntity);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            User = MapToDto(user)
        };

        return (response, refreshToken, null);
    }

    public async Task<(AuthResponse? Response, string? NewRefreshToken, string? Error)> RefreshAsync(string refreshToken)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);

        if (storedToken is null)
            return (null, null, "Invalid refresh token.");

        // If the token was already revoked, this might be a stolen token being reused.
        // Revoke all tokens for this user as a safety measure.
        if (storedToken.IsRevoked)
        {
            await _refreshTokenRepo.RevokeAllForUserAsync(storedToken.UserId);
            return (null, null, "Token has been revoked. All sessions invalidated for security.");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return (null, null, "Refresh token has expired.");

        // Look up the user
        var user = await _userRepo.GetByIdAsync(storedToken.UserId);
        if (user is null)
            return (null, null, "User not found.");

        // Rotate: generate new refresh token, revoke old one
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newTokenHash = _tokenService.HashToken(newRefreshToken);

        // Mark old token as revoked with a pointer to its replacement
        await _refreshTokenRepo.RevokeAsync(storedToken.Id, newTokenHash);

        // Store new token
        var newTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        };
        await _refreshTokenRepo.CreateAsync(newTokenEntity);

        // Generate new access token
        var accessToken = _tokenService.GenerateAccessToken(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            User = MapToDto(user)
        };

        return (response, newRefreshToken, null);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _refreshTokenRepo.GetByTokenHashAsync(tokenHash);

        if (storedToken is not null && !storedToken.IsRevoked)
        {
            await _refreshTokenRepo.RevokeAsync(storedToken.Id);
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id.ToString(),
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Points = user.Points
        };
    }
}
