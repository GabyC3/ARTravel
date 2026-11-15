using System.ComponentModel.DataAnnotations;
using ARTravel.Domain.Entities;

namespace ARTravel.API.Contracts;

public sealed class CreateReservationRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    [Range(1, int.MaxValue)]
    public int DepartureId { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class UpdateReservationRequest
{
    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Discount { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class SetReservationStatusRequest
{
    [Required]
    public ReservationStatus? Status { get; init; }
}

public sealed class CancelReservationRequest
{
    [Required, MaxLength(2000)]
    public string Reason { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal RefundAmount { get; init; }
}

public class ReservationPassengerRequest
{
    [Range(1, int.MaxValue)]
    public int? PassengerId { get; init; }

    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    public ReservationDocumentType? DocumentType { get; init; }

    [Required, MaxLength(100)]
    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    [MaxLength(50)]
    public string? Gender { get; init; }

    [MaxLength(30)]
    public string? PhoneNumber { get; init; }

    [EmailAddress, MaxLength(320)]
    public string? Email { get; init; }

    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(30)]
    public string? EmergencyContactPhone { get; init; }

    [MaxLength(2000)]
    public string? SpecialRequirements { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public DeparturePriceType PriceType { get; init; } = DeparturePriceType.Standard;
}

public sealed class CreateReservationPassengerRequest : ReservationPassengerRequest
{
}

public sealed class UpdateReservationPassengerRequest : ReservationPassengerRequest
{
}
