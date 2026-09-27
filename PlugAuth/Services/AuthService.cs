using Microsoft.Extensions.Options;
using PlugAuth.Configuration;
using PlugAuth.DTOs;
using PlugAuth.Entities;
using PlugAuth.Stores;

namespace PlugAuth.Services;

public class AuthService : IAuthService
{
    private readonly IAuthUserStore _userStore;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IAuthUserStore userStore,
        IPasswordService passwordService,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _userStore = userStore;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        if (await _userStore.EmailExistsAsync(email))
            throw new InvalidOperationException("Email is already registered.");

        var user = new AuthUser
        {
            Email = email,
            PasswordHash = string.Empty,
            DisplayName = request.DisplayName?.Trim()
        };

        user.PasswordHash =
            _passwordService.HashPassword(user, request.Password);

        await _userStore.CreateUserAsync(user);

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userStore.FindByEmailAsync(email);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var passwordValid = _passwordService.VerifyPassword(
            user,
            request.Password,
            user.PasswordHash);

        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var tokenHash =
            _tokenService.HashRefreshToken(refreshToken);

        var storedToken =
            await _userStore.FindRefreshTokenAsync(tokenHash);

        if (storedToken == null ||
            storedToken.IsRevoked ||
            storedToken.IsExpired)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        var user =
            await _userStore.FindByIdAsync(storedToken.UserId);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid user.");

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        await _userStore.UpdateRefreshTokenAsync(storedToken);

        return await CreateAuthResponseAsync(user);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var tokenHash =
            _tokenService.HashRefreshToken(refreshToken);

        var storedToken =
            await _userStore.FindRefreshTokenAsync(tokenHash);

        if (storedToken == null || storedToken.IsRevoked)
            return;

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        await _userStore.UpdateRefreshTokenAsync(storedToken);
    }

    public Task<AuthUser?> GetUserAsync(Guid userId)
    {
        return _userStore.FindByIdAsync(userId);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        AuthUser user)
    {
        var accessToken =
            _tokenService.GenerateAccessToken(user);

        var refreshToken =
            _tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash =
                _tokenService.HashRefreshToken(refreshToken),
            ExpiresAtUtc =
                DateTime.UtcNow.AddDays(
                    _jwtOptions.RefreshTokenExpirationDays)
        };

        await _userStore.SaveRefreshTokenAsync(
            refreshTokenEntity);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,

            AccessTokenExpiresAtUtc =
                DateTime.UtcNow.AddMinutes(
                    _jwtOptions.AccessTokenExpirationMinutes),

            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        };
    }
}