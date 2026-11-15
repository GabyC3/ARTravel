using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Reservations;

public interface IReservationStore
{
    Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        string? search,
        int? customerId,
        int? departureId,
        ReservationStatus? status,
        DateTime? reservationDateFrom,
        DateTime? reservationDateTo,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByCustomerAsync(
        int customerId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByDepartureAsync(
        int departureId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<Reservation?> FindAsync(int id, CancellationToken cancellationToken);

    Task<Customer?> FindCustomerAsync(int customerId, CancellationToken cancellationToken);

    Task<Departure?> FindDepartureAsync(int departureId, CancellationToken cancellationToken);

    Task<DeparturePrice?> FindDeparturePriceAsync(
        int departureId,
        DeparturePriceType priceType,
        CancellationToken cancellationToken);

    Task<Passenger?> FindPassengerAsync(int passengerId, CancellationToken cancellationToken);

    Task<int> CountActivePassengersForDepartureAsync(
        int departureId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ReservationPassenger> Items, int TotalCount)> ListPassengersAsync(
        int reservationId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<ReservationPassenger?> FindReservationPassengerAsync(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken);

    Task<bool> HasTripForReservationPassengerAsync(
        int passengerId,
        CancellationToken cancellationToken);

    Task<bool> PassengerDocumentExistsAsync(
        int reservationId,
        ReservationDocumentType documentType,
        string documentNumber,
        int? excludingPassengerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReservationPayment>> ListPaymentsAsync(
        int reservationId,
        CancellationToken cancellationToken);

    Task<decimal> SumPaidPaymentsAsync(int reservationId, CancellationToken cancellationToken);

    Task<bool> AddPassengerIfCapacityAsync(
        ReservationPassenger passenger,
        int departureId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReservationPassenger>> ListDeparturePassengersAsync(
        int departureId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    void Add(Reservation reservation);

    void Add(ReservationCancellation cancellation);

    void Remove(ReservationPassenger passenger);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
