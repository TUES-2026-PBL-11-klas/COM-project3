using Core.DTOs;

namespace Core.Services;

public interface IAuthService
{
    Task<(AuthResponse? Response, string? Error)> SignupAsync(SignupRequest request);
    Task<(AuthResponse? Response, string? RefreshToken, string? Error)> LoginAsync(LoginRequest request);
    Task<(AuthResponse? Response, string? NewRefreshToken, string? Error)> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
