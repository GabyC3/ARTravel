namespace ARTravel.Domain.Entities;

public sealed class Departure
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int ItineraryId { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime ReturnDate { get; set; }

    public required string MeetingPoint { get; set; }

    public TimeOnly? MeetingTime { get; set; }

    public int Capacity { get; set; }

    public int MinimumPassengers { get; set; }

    public decimal BasePrice { get; set; }

    public required string Currency { get; set; }

    public DepartureStatus Status { get; set; } = DepartureStatus.Scheduled;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public TourismProduct Product { get; set; } = null!;

    public Itinerary Itinerary { get; set; } = null!;

    public ICollection<DeparturePrice> Prices { get; set; } = new List<DeparturePrice>();

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
