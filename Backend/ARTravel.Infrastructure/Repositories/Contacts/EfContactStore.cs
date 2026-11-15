using ARTravel.Application.Interfaces.Contacts;
using ARTravel.Domain.Entities;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ARTravel.Infrastructure.Repositories.Contacts;

public sealed class EfContactStore(AppDbContext dbContext) : IContactStore
{
    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListCustomersAsync(
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Customers.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(customer => customer.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(customer =>
                EF.Functions.ILike(customer.FirstName ?? string.Empty, pattern) ||
                EF.Functions.ILike(customer.LastName ?? string.Empty, pattern) ||
                EF.Functions.ILike(customer.BusinessName ?? string.Empty, pattern) ||
                EF.Functions.ILike(customer.DocumentNumber, pattern) ||
                EF.Functions.ILike(customer.Email ?? string.Empty, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(customer => customer.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<Customer?> FindCustomerAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Customers.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<int> CountCustomerReservationsAsync(
        int customerId,
        CancellationToken cancellationToken) =>
        dbContext.Reservations.CountAsync(
            reservation => reservation.CustomerId == customerId,
            cancellationToken);

    public async Task<decimal> SumCustomerReservationAmountsAsync(
        int customerId,
        CancellationToken cancellationToken) =>
        await dbContext.Reservations
            .Where(reservation => reservation.CustomerId == customerId)
            .SumAsync(reservation => (decimal?)reservation.TotalAmount, cancellationToken)
            ?? 0m;

    public async Task<(IReadOnlyList<Passenger> Items, int TotalCount)> ListPassengersAsync(
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Passengers.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(passenger => passenger.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(passenger =>
                EF.Functions.ILike(passenger.FirstName, pattern) ||
                EF.Functions.ILike(passenger.LastName, pattern) ||
                EF.Functions.ILike(passenger.DocumentNumber, pattern) ||
                EF.Functions.ILike(passenger.Email ?? string.Empty, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(passenger => passenger.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<Passenger?> FindPassengerAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Passengers.SingleOrDefaultAsync(passenger => passenger.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Trip> Items, int TotalCount)> GetPassengerTripsAsync(
        int passengerId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Trips
            .AsNoTracking()
            .Where(trip => trip.PassengerId == passengerId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(trip => trip.DepartureAt)
            .ThenByDescending(trip => trip.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public void Add(Customer customer) => dbContext.Customers.Add(customer);

    public void Add(Passenger passenger) => dbContext.Passengers.Add(passenger);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
