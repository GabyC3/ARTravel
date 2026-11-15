using System.ComponentModel.DataAnnotations;
using ARTravel.Domain.Entities;

namespace ARTravel.API.Contracts;

public sealed class CreateItineraryRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }
}

public sealed class UpdateItineraryRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }
}

public sealed class CreateItineraryDayRequest
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }
}

public sealed class UpdateItineraryDayRequest
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }
}

public sealed class ReorderItineraryDaysRequest
{
    [Required]
    public IReadOnlyList<int>? DayIds { get; init; }
}

public sealed class AddItineraryActivityRequest
{
    [Range(1, int.MaxValue)]
    public int DestinationId { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    public ItineraryActivityType ActivityType { get; init; }

    public TimeOnly? StartTime { get; init; }

    public TimeOnly? EndTime { get; init; }

    [Range(0, int.MaxValue)]
    public int DurationMinutes { get; init; }

    [Range(0, int.MaxValue)]
    public int? DisplayOrder { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class UpdateItineraryActivityRequest
{
    [Range(1, int.MaxValue)]
    public int DestinationId { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    public ItineraryActivityType ActivityType { get; init; }

    public TimeOnly? StartTime { get; init; }

    public TimeOnly? EndTime { get; init; }

    [Range(0, int.MaxValue)]
    public int DurationMinutes { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class ReorderItineraryActivitiesRequest
{
    [Required]
    public IReadOnlyList<int>? ActivityIds { get; init; }
}
