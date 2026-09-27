using PlugAuth.Entities;

namespace PlugAuth.Services;

public interface ITokenService
{
    string GenerateAccessToken(AuthUser user);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}