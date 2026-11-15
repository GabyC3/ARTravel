using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Itineraries;

public interface IItineraryStore
{
    Task<(IReadOnlyList<Itinerary> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        int? productId,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken);

    Task<Itinerary?> FindItineraryAsync(int id, CancellationToken cancellationToken);

    Task<ItineraryDay?> FindDayAsync(int dayId, CancellationToken cancellationToken);

    Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItineraryDay>> GetDaysAsync(
        int itineraryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ItineraryActivity>> GetActivitiesAsync(
        int dayId,
        CancellationToken cancellationToken);

    Task<bool> DestinationExistsAsync(int destinationId, CancellationToken cancellationToken);

    void Add(Itinerary itinerary);

    void Add(ItineraryDay day);

    void Add(ItineraryActivity activity);

    void Remove(ItineraryDay day);

    void Remove(ItineraryActivity activity);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
