namespace ARTravel.Domain.Entities;

public sealed class ItineraryActivity
{
    public int Id { get; set; }

    public int ItineraryDayId { get; set; }

    public int DestinationId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ItineraryActivityType ActivityType { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int DurationMinutes { get; set; }

    public int DisplayOrder { get; set; }

    public string? Notes { get; set; }

    public ItineraryDay ItineraryDay { get; set; } = null!;

}
