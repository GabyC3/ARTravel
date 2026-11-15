namespace ARTravel.Domain.Entities;

public sealed class TourismProduct
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Code { get; set; }

    public string? Description { get; set; }

    public ProductType ProductType { get; set; }

    public int DurationDays { get; set; }

    public int DurationNights { get; set; }

    public decimal BasePrice { get; set; }

    public required string Currency { get; set; }

    public int MinimumPassengers { get; set; }

    public int? MaximumPassengers { get; set; }

    public string? Includes { get; set; }

    public string? Excludes { get; set; }

    public string? Recommendations { get; set; }

    public string? CancellationPolicy { get; set; }

    public bool IsPublished { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Itinerary> Itineraries { get; set; } = new List<Itinerary>();
}
