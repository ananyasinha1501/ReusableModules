using PlugAuth.DTOs;
using PlugAuth.Entities;

namespace PlugAuth.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<AuthResponse> RefreshAsync(string refreshToken);

    Task RevokeRefreshTokenAsync(string refreshToken);

    Task<AuthUser?> GetUserAsync(Guid userId);
}