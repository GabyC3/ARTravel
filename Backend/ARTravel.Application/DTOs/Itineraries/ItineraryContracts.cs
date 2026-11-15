using ARTravel.Domain.Entities;

namespace ARTravel.Application.DTOs.Itineraries;

public sealed record CreateItineraryCommand(int ProductId, string Name, string? Description);

public sealed record UpdateItineraryCommand(
    int ProductId,
    string Name,
    string? Description);

public sealed record AddItineraryDayCommand(string Title, string? Description);

public sealed record UpdateItineraryDayCommand(string Title, string? Description);

public sealed record AddItineraryActivityCommand(
    int DestinationId,
    string Name,
    string? Description,
    ItineraryActivityType ActivityType,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int DurationMinutes,
    int? DisplayOrder,
    string? Notes);

public sealed record UpdateItineraryActivityCommand(
    int DestinationId,
    string Name,
    string? Description,
    ItineraryActivityType ActivityType,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int DurationMinutes,
    string? Notes);

public sealed record ItineraryResponse(
    int Id,
    int ProductId,
    string Name,
    string? Description,
    int Version,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ItineraryDetailResponse(
    ItineraryResponse Itinerary,
    IReadOnlyList<ItineraryDayResponse> Days);

public sealed record ItineraryDayResponse(
    int Id,
    int ItineraryId,
    int DayNumber,
    string Title,
    string? Description,
    DateTime CreatedAt,
    IReadOnlyList<ItineraryActivityResponse> Activities);

public sealed record ItineraryActivityResponse(
    int Id,
    int ItineraryDayId,
    int DestinationId,
    string Name,
    string? Description,
    ItineraryActivityType ActivityType,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int DurationMinutes,
    int DisplayOrder,
    string? Notes);

public sealed record PagedItineraries(
    IReadOnlyList<ItineraryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
