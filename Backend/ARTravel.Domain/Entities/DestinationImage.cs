namespace ARTravel.Domain.Entities;

public sealed class DestinationImage
{
    public int Id { get; set; }

    public int DestinationId { get; set; }

    public required string ImageUrl { get; set; }

    public string? AltText { get; set; }

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}
