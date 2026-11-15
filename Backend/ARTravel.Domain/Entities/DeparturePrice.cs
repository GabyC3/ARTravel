namespace ARTravel.Domain.Entities;

public sealed class DeparturePrice
{
    public int Id { get; set; }

    public int DepartureId { get; set; }

    public DeparturePriceType PriceType { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public string? Description { get; set; }

    public Departure Departure { get; set; } = null!;
}
