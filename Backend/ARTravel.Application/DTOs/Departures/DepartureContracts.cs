using ARTravel.Domain.Entities;

namespace ARTravel.Application.DTOs.Departures;

public sealed record CreateDepartureCommand(
    int ProductId,
    int ItineraryId,
    DateTime DepartureDate,
    DateTime ReturnDate,
    string MeetingPoint,
    TimeOnly? MeetingTime,
    int Capacity,
    int MinimumPassengers,
    decimal BasePrice,
    string Currency,
    DepartureStatus Status,
    string? Notes);

public sealed record UpdateDepartureCommand(
    int ItineraryId,
    DateTime DepartureDate,
    DateTime ReturnDate,
    string MeetingPoint,
    TimeOnly? MeetingTime,
    int Capacity,
    int MinimumPassengers,
    decimal BasePrice,
    string Currency,
    string? Notes);

public sealed record DepartureResponse(
    int Id,
    int ProductId,
    int ItineraryId,
    DateTime DepartureDate,
    DateTime ReturnDate,
    string MeetingPoint,
    TimeOnly? MeetingTime,
    int Capacity,
    int MinimumPassengers,
    decimal BasePrice,
    string Currency,
    DepartureStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record PagedDepartures(
    IReadOnlyList<DepartureResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record DepartureAvailability(
    int DepartureId,
    int Capacity,
    int PassengerCount,
    int AvailableSeats,
    int MinimumPassengers,
    bool IsFull,
    DepartureStatus Status);

public sealed record CreateDeparturePriceCommand(
    DeparturePriceType PriceType,
    decimal Amount,
    string Currency,
    string? Description);

public sealed record DeparturePriceResponse(
    int Id,
    int DepartureId,
    DeparturePriceType PriceType,
    decimal Amount,
    string Currency,
    string? Description);
