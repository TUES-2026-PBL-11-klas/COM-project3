using Core.DTOs;
using Core.Users;

namespace Core.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}
