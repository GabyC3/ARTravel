namespace ARTravel.Domain.Entities;

public sealed class TourismProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public required string ImageUrl { get; set; }

    public string? AltText { get; set; }

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}
