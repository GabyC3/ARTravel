namespace ARTravel.Domain.Entities;

public sealed class Reservation
{
    public int Id { get; set; }

    public required string ReservationNumber { get; set; }

    public int CustomerId { get; set; }

    public int DepartureId { get; set; }

    public DateTime ReservationDate { get; set; }

    public int NumberOfPassengers { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal TotalAmount { get; set; }

    public required string Currency { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;

    public Departure Departure { get; set; } = null!;

    public ICollection<ReservationPassenger> Passengers { get; set; } = new List<ReservationPassenger>();

    public ICollection<ReservationCancellation> Cancellations { get; set; } =
        new List<ReservationCancellation>();

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
