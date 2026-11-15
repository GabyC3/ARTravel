namespace ARTravel.Domain.Entities;

public sealed class ItineraryDay
{
    public int Id { get; set; }

    public int ItineraryId { get; set; }

    public int DayNumber { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public Itinerary Itinerary { get; set; } = null!;

    public ICollection<ItineraryActivity> Activities { get; set; } = new List<ItineraryActivity>();
}
