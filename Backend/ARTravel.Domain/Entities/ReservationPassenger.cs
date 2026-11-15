namespace ARTravel.Domain.Entities;

public sealed class ReservationPassenger
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public int? PassengerId { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public ReservationDocumentType DocumentType { get; set; }

    public required string DocumentNumber { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? SpecialRequirements { get; set; }

    public string? Notes { get; set; }

    public DeparturePriceType PriceType { get; set; }

    public decimal UnitPrice { get; set; }

    public required string Currency { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;

    public Passenger? Passenger { get; set; }

    public Trip? Trip { get; set; }
}
