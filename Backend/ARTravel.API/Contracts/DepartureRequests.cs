using System.ComponentModel.DataAnnotations;
using ARTravel.Domain.Entities;

namespace ARTravel.API.Contracts;

public sealed class CreateDepartureRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int ItineraryId { get; init; }

    public DateTime DepartureDate { get; init; }

    public DateTime ReturnDate { get; init; }

    [Required, MaxLength(300)]
    public string MeetingPoint { get; init; } = string.Empty;

    public TimeOnly? MeetingTime { get; init; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    [Range(1, int.MaxValue)]
    public int MinimumPassengers { get; init; } = 1;

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal BasePrice { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    public DepartureStatus Status { get; init; } = DepartureStatus.Scheduled;

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class CreateProductDepartureRequest
{
    [Range(1, int.MaxValue)]
    public int ItineraryId { get; init; }

    public DateTime DepartureDate { get; init; }

    public DateTime ReturnDate { get; init; }

    [Required, MaxLength(300)]
    public string MeetingPoint { get; init; } = string.Empty;

    public TimeOnly? MeetingTime { get; init; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    [Range(1, int.MaxValue)]
    public int MinimumPassengers { get; init; } = 1;

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal BasePrice { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    public DepartureStatus Status { get; init; } = DepartureStatus.Scheduled;

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class UpdateDepartureRequest
{
    [Range(1, int.MaxValue)]
    public int ItineraryId { get; init; }

    public DateTime DepartureDate { get; init; }

    public DateTime ReturnDate { get; init; }

    [Required, MaxLength(300)]
    public string MeetingPoint { get; init; } = string.Empty;

    public TimeOnly? MeetingTime { get; init; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }

    [Range(1, int.MaxValue)]
    public int MinimumPassengers { get; init; } = 1;

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal BasePrice { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class SetDepartureStatusRequest
{
    [Required]
    public DepartureStatus? Status { get; init; }
}

public sealed class AddDeparturePriceRequest
{
    [Required]
    public DeparturePriceType? PriceType { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Amount { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }
}
