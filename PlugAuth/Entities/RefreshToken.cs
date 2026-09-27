namespace PlugAuth.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string TokenHash { get; set; }

    public Guid UserId { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsRevoked => RevokedAtUtc != null;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}