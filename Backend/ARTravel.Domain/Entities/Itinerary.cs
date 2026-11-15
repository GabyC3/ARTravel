namespace ARTravel.Domain.Entities;

public sealed class Itinerary
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public TourismProduct Product { get; set; } = null!;

    public ICollection<ItineraryDay> Days { get; set; } = new List<ItineraryDay>();
}
