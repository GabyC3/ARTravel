using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Departures;

public interface IDepartureStore
{
    Task<(IReadOnlyList<Departure> Items, int TotalCount)> ListAsync(
        int? productId,
        DepartureStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<Departure?> FindAsync(int id, CancellationToken cancellationToken);

    Task<TourismProduct?> FindProductAsync(int productId, CancellationToken cancellationToken);

    Task<Itinerary?> FindItineraryAsync(int itineraryId, CancellationToken cancellationToken);

    Task<int> CountPassengersAsync(int departureId, CancellationToken cancellationToken);

    Task<bool> UpdateIfCapacityAllowsAsync(
        Departure departure,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DeparturePrice>> ListPricesAsync(
        int departureId,
        CancellationToken cancellationToken);

    Task<DeparturePrice?> FindPriceAsync(int priceId, CancellationToken cancellationToken);

    void Add(Departure departure);

    void Add(DeparturePrice price);

    void Remove(DeparturePrice price);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
