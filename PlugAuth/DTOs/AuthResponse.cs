namespace PlugAuth.DTOs;

using PlugAuth.Entities;

public class AuthResponse
{
    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public Guid UserId { get; set; }

    public required string Email { get; set; }

    public string? DisplayName { get; set; }

    public UserRole Role { get; set; } = UserRole.Buyer;
}