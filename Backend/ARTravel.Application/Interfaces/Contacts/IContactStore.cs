using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Contacts;

public interface IContactStore
{
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListCustomersAsync(
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<Customer?> FindCustomerAsync(int id, CancellationToken cancellationToken);

    Task<int> CountCustomerReservationsAsync(int customerId, CancellationToken cancellationToken);

    Task<decimal> SumCustomerReservationAmountsAsync(
        int customerId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Passenger> Items, int TotalCount)> ListPassengersAsync(
        int skip,
        int take,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<Passenger?> FindPassengerAsync(int id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Trip> Items, int TotalCount)> GetPassengerTripsAsync(
        int passengerId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    void Add(Customer customer);

    void Add(Passenger passenger);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
