using System.ComponentModel.DataAnnotations;
using ARTravel.Domain.Entities;

namespace ARTravel.API.Contracts;

public sealed class CreateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(50)]
    public string Code { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    public ProductType ProductType { get; init; }

    [Range(0, int.MaxValue)]
    public int DurationDays { get; init; }

    [Range(0, int.MaxValue)]
    public int DurationNights { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal BasePrice { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MinimumPassengers { get; init; } = 1;

    [Range(1, int.MaxValue)]
    public int? MaximumPassengers { get; init; }

    [MaxLength(4000)]
    public string? Includes { get; init; }

    [MaxLength(4000)]
    public string? Excludes { get; init; }

    [MaxLength(4000)]
    public string? Recommendations { get; init; }

    [MaxLength(4000)]
    public string? CancellationPolicy { get; init; }

    public bool IsPublished { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(50)]
    public string Code { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    public ProductType ProductType { get; init; }

    [Range(0, int.MaxValue)]
    public int DurationDays { get; init; }

    [Range(0, int.MaxValue)]
    public int DurationNights { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal BasePrice { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MinimumPassengers { get; init; } = 1;

    [Range(1, int.MaxValue)]
    public int? MaximumPassengers { get; init; }

    [MaxLength(4000)]
    public string? Includes { get; init; }

    [MaxLength(4000)]
    public string? Excludes { get; init; }

    [MaxLength(4000)]
    public string? Recommendations { get; init; }

    [MaxLength(4000)]
    public string? CancellationPolicy { get; init; }
}

public sealed class SetProductPublicationRequest
{
    [Required]
    public bool? IsPublished { get; init; }
}

public sealed class ProductDestinationItemRequest
{
    [Range(1, int.MaxValue)]
    public int DestinationId { get; init; }

    [Range(1, int.MaxValue)]
    public int VisitOrder { get; init; }

    public bool IsMainDestination { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

public sealed class AssignProductDestinationsRequest
{
    [Required]
    public IReadOnlyList<ProductDestinationItemRequest>? Destinations { get; init; }
}

public sealed class AddProductImageRequest
{
    [Required, Url, MaxLength(2048)]
    public string ImageUrl { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? AltText { get; init; }

    public bool IsPrimary { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }
}
