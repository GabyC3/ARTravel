using System.ComponentModel.DataAnnotations;
using ARTravel.Domain.Entities;

namespace ARTravel.API.Contracts;

public sealed class CreateDestinationRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public DestinationType DestinationType { get; init; }

    [Required, MaxLength(100)]
    public string Country { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Province { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; init; }

    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; init; }

    [Range(0, int.MaxValue)]
    public int? AltitudeMeters { get; init; }

    [Range(1, int.MaxValue)]
    public int? ParentDestinationId { get; init; }

    [MaxLength(10)]
    public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateDestinationRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public DestinationType DestinationType { get; init; }

    [Required, MaxLength(100)]
    public string Country { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Province { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; init; }

    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; init; }

    [Range(0, int.MaxValue)]
    public int? AltitudeMeters { get; init; }

    [Range(1, int.MaxValue)]
    public int? ParentDestinationId { get; init; }
}

public sealed class AddDestinationImageRequest
{
    [Required, Url, MaxLength(2048)]
    public string ImageUrl { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? AltText { get; init; }

    public bool IsPrimary { get; init; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; init; }
}
