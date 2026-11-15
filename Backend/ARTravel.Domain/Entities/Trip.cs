namespace ARTravel.Domain.Entities;

public sealed class Trip
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public int? PassengerId { get; set; }

    public int? DepartureId { get; set; }

    public int? ReservationPassengerId { get; set; }

    public required string Origin { get; set; }

    public required string Destination { get; set; }

    public DateTime DepartureAt { get; set; }

    public DateTime? ArrivalAt { get; set; }

    public required string Status { get; set; }

    public Departure? Departure { get; set; }

    public Passenger? Passenger { get; set; }

    public ReservationPassenger? ReservationPassenger { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
