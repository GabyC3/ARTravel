namespace ARTravel.Domain.Entities;

public sealed class Destination
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public DestinationType DestinationType { get; set; }

    public required string Country { get; set; }

    public string? Province { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int? AltitudeMeters { get; set; }

    public int? ParentDestinationId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
