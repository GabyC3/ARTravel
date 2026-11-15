using ARTravel.Application.Interfaces.Departures;
using ARTravel.Domain.Entities;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ARTravel.Infrastructure.Repositories.Departures;

public sealed class EfDepartureStore(AppDbContext dbContext) : IDepartureStore
{
    private static readonly ReservationStatus[] CapacityStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed,
        ReservationStatus.PartiallyPaid,
        ReservationStatus.Paid
    ];

    public async Task<(IReadOnlyList<Departure> Items, int TotalCount)> ListAsync(
        int? productId,
        DepartureStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Departures.AsNoTracking();
        if (productId.HasValue)
        {
            query = query.Where(departure => departure.ProductId == productId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(departure => departure.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(departure => departure.DepartureDate)
            .ThenBy(departure => departure.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<Departure?> FindAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Departures.SingleOrDefaultAsync(departure => departure.Id == id, cancellationToken);

    public Task<TourismProduct?> FindProductAsync(
        int productId,
        CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);

    public Task<Itinerary?> FindItineraryAsync(
        int itineraryId,
        CancellationToken cancellationToken) =>
        dbContext.Itineraries.AsNoTracking()
            .SingleOrDefaultAsync(itinerary => itinerary.Id == itineraryId, cancellationToken);

    public Task<int> CountPassengersAsync(int departureId, CancellationToken cancellationToken) =>
        dbContext.ReservationPassengers
            .Where(passenger =>
                passenger.Reservation.DepartureId == departureId &&
                CapacityStatuses.Contains(passenger.Reservation.Status))
            .CountAsync(cancellationToken);

    public async Task<bool> UpdateIfCapacityAllowsAsync(
        Departure departure,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        _ = await dbContext.Departures
            .FromSqlInterpolated(
                $"SELECT * FROM \"Departures\" WHERE \"Id\" = {departure.Id} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Departure {departure.Id} disappeared during update.");

        var passengerCount = await CountPassengersAsync(departure.Id, cancellationToken);
        if (departure.Capacity < passengerCount)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DeparturePrice>> ListPricesAsync(
        int departureId,
        CancellationToken cancellationToken) =>
        await dbContext.DeparturePrices
            .AsNoTracking()
            .Where(price => price.DepartureId == departureId)
            .OrderBy(price => price.PriceType)
            .ThenBy(price => price.Id)
            .ToListAsync(cancellationToken);

    public Task<DeparturePrice?> FindPriceAsync(int priceId, CancellationToken cancellationToken) =>
        dbContext.DeparturePrices.SingleOrDefaultAsync(price => price.Id == priceId, cancellationToken);

    public void Add(Departure departure) => dbContext.Departures.Add(departure);

    public void Add(DeparturePrice price) => dbContext.DeparturePrices.Add(price);

    public void Remove(DeparturePrice price) => dbContext.DeparturePrices.Remove(price);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

}
