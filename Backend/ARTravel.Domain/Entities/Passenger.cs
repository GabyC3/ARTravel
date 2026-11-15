namespace ARTravel.Domain.Entities;

public sealed class Passenger
{
    public int Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string DocumentType { get; set; }

    public required string DocumentNumber { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Nationality { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public DateOnly? PassportExpirationDate { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ReservationPassenger> ReservationPassengers { get; set; } =
        new List<ReservationPassenger>();
}
