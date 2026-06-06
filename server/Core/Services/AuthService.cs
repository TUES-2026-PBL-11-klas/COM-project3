using System.Text.RegularExpressions;
using Core.Configuration;
using Core.DTOs;
using Core.Repositories;
using Core.Users;

namespace Core.Services;

public sealed partial class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public AuthService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<(AuthResponse? Response, string? Error)> SignupAsync(SignupRequest request)
    {
        if (!EmailRegex().IsMatch(request.Email))
            return (null, "Invalid email format.");

        var (isValid, error) = _passwordService.ValidateComplexity(request.Password);
        if (!isValid)
            return (null, error);

        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3)
            return (null, "Username must be at least 3 characters.");

        if (request.Username.Length > 30)
            return (null, "Username must be at most 30 characters.");

        if (await _userRepository.ExistsByEmailAsync(request.Email))
            return (null, "An account with this email already exists.");

        if (await _userRepository.ExistsByUsernameAsync(request.Username))
            return (null, "This username is already taken.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordService.Hash(request.Password),
            Role = "citizen"
        };

        var createdUser = await _userRepository.CreateAsync(user);
        var token = _tokenService.GenerateAccessToken(createdUser);
        return (new AuthResponse
        {
            AccessToken = token,
            User = MapToDto(createdUser)
        }, null);
    }

    public async Task<(AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordService.Verify(request.Password, user.PasswordHash))
            return (null, "Invalid email or password.");

        var token = _tokenService.GenerateAccessToken(user);
        return (new AuthResponse
        {
            AccessToken = token,
            User = MapToDto(user)
        }, null);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id.ToString(),
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        Points = user.Points
    };
}
