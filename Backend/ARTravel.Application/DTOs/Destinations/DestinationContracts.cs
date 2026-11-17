using ARTravel.Domain.Entities;

namespace ARTravel.Application.DTOs.Destinations;

public sealed record CreateDestinationCommand(
    string Name,
    string? Description,
    DestinationType DestinationType,
    string Country,
    string? Province,
    string? City,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    int? AltitudeMeters,
    int? ParentDestinationId,
    bool IsActive,
    IReadOnlyList<string> ImageUrls);

public sealed record UpdateDestinationCommand(
    string Name,
    string? Description,
    DestinationType DestinationType,
    string Country,
    string? Province,
    string? City,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    int? AltitudeMeters,
    int? ParentDestinationId);

public sealed record AddDestinationImageCommand(
    string ImageUrl,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder);

public sealed record DestinationResponse(
    int Id,
    string Name,
    string? Description,
    DestinationType DestinationType,
    string Country,
    string? Province,
    string? City,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    int? AltitudeMeters,
    int? ParentDestinationId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? PrimaryImageUrl);

public sealed record DestinationDetail(
    DestinationResponse Destination,
    IReadOnlyList<DestinationImageResponse> Images);

public sealed record DestinationImageResponse(
    int Id,
    int DestinationId,
    string ImageUrl,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder,
    DateTime CreatedAt);

public sealed record PagedDestinations(
    IReadOnlyList<DestinationResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record DestinationSummary(
    DestinationResponse Destination,
    int ChildDestinationCount,
    int ProductCount,
    int ImageCount);
