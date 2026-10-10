namespace PlugAuth.DTOs;

using PlugAuth.Entities;

public class RegisterRequest
{
    public required string Email { get; set; }

    public required string Password { get; set; }

    public string? DisplayName { get; set; }

    public UserRole Role { get; set; } = UserRole.Buyer;
}