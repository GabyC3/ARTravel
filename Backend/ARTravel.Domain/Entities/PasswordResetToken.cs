namespace ARTravel.Domain.Entities;

public class PasswordResetToken
{
    public required string TokenHash { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }
}
