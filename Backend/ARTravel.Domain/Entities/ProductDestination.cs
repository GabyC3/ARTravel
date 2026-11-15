namespace ARTravel.Domain.Entities;

public sealed class ProductDestination
{
    public int ProductId { get; set; }

    public int DestinationId { get; set; }

    public int VisitOrder { get; set; }

    public bool IsMainDestination { get; set; }

    public string? Notes { get; set; }

    public TourismProduct TourismProduct { get; set; } = null!;

    public Destination Destination { get; set; } = null!;
}
