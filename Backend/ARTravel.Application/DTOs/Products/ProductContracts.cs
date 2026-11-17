using ARTravel.Domain.Entities;

namespace ARTravel.Application.DTOs.Products;

public sealed record CreateProductCommand(
    string Name,
    string Code,
    string? Description,
    ProductType ProductType,
    int DurationDays,
    int DurationNights,
    decimal BasePrice,
    string Currency,
    int MinimumPassengers,
    int? MaximumPassengers,
    string? Includes,
    string? Excludes,
    string? Recommendations,
    string? CancellationPolicy,
    bool IsPublished,
    bool IsActive);

public sealed record UpdateProductCommand(
    string Name,
    string Code,
    string? Description,
    ProductType ProductType,
    int DurationDays,
    int DurationNights,
    decimal BasePrice,
    string Currency,
    int MinimumPassengers,
    int? MaximumPassengers,
    string? Includes,
    string? Excludes,
    string? Recommendations,
    string? CancellationPolicy);

public sealed record AssignProductDestinationItem(
    int DestinationId,
    int VisitOrder,
    bool IsMainDestination,
    string? Notes);

public sealed record TourismProductResponse(
    int Id,
    string Name,
    string Code,
    string? Description,
    ProductType ProductType,
    int DurationDays,
    int DurationNights,
    decimal BasePrice,
    string Currency,
    int MinimumPassengers,
    int? MaximumPassengers,
    string? Includes,
    string? Excludes,
    string? Recommendations,
    string? CancellationPolicy,
    bool IsPublished,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? PrimaryImageUrl = null);

public sealed record ProductDetail(
    TourismProductResponse Product,
    IReadOnlyList<ProductDestinationResponse> Destinations,
    IReadOnlyList<ProductImageResponse> Images);

public sealed record ProductDestinationResponse(
    int ProductId,
    int DestinationId,
    int VisitOrder,
    bool IsMainDestination,
    string? Notes,
    string DestinationName,
    string Country,
    string? City,
    DestinationType DestinationType,
    string? Province = null);

public sealed record ProductImageResponse(
    int Id,
    int ProductId,
    string ImageUrl,
    string? AltText,
    bool IsPrimary,
    int DisplayOrder,
    DateTime CreatedAt);

public sealed record PagedProducts(
    IReadOnlyList<TourismProductResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

