using PlugAuth.Entities;

namespace PlugAuth.Stores;

public interface IAuthUserStore
{
    Task<AuthUser?> FindByEmailAsync(string email);

    Task<AuthUser?> FindByIdAsync(Guid userId);

    Task<bool> EmailExistsAsync(string email);

    Task CreateUserAsync(AuthUser user);

    Task SaveRefreshTokenAsync(RefreshToken refreshToken);

    Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash);

    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
}