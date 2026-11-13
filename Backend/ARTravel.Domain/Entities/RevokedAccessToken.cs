namespace ARTravel.Domain.Entities;

public class RevokedAccessToken
{
    public required string TokenId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime RevokedAt { get; set; }
}
