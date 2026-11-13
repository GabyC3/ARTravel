namespace ARTravel.Domain.Entities;

public class RefreshToken
{
    public required string TokenHash { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public required string SessionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
