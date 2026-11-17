using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Reservations;
using ARTravel.Domain.Entities;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ARTravel.Infrastructure.Repositories.Reservations;

public sealed class EfReservationStore(AppDbContext dbContext) : IReservationStore
{
    private static readonly ReservationStatus[] CapacityStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed,
        ReservationStatus.PartiallyPaid,
        ReservationStatus.Paid
    ];

    public async Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        string? search,
        int? customerId,
        int? departureId,
        ReservationStatus? status,
        DateTime? reservationDateFrom,
        DateTime? reservationDateTo,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Reservations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(reservation =>
                EF.Functions.ILike(reservation.ReservationNumber, pattern) ||
                EF.Functions.ILike(reservation.Customer.FirstName ?? string.Empty, pattern) ||
                EF.Functions.ILike(reservation.Customer.LastName ?? string.Empty, pattern) ||
                EF.Functions.ILike(reservation.Customer.BusinessName ?? string.Empty, pattern) ||
                EF.Functions.ILike(reservation.Customer.DocumentNumber, pattern) ||
                EF.Functions.ILike(reservation.Customer.Email ?? string.Empty, pattern));
        }

        if (customerId.HasValue)
        {
            query = query.Where(reservation => reservation.CustomerId == customerId.Value);
        }

        if (departureId.HasValue)
        {
            query = query.Where(reservation => reservation.DepartureId == departureId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(reservation => reservation.Status == status.Value);
        }

        if (reservationDateFrom.HasValue)
        {
            query = query.Where(reservation => reservation.ReservationDate >= reservationDateFrom.Value);
        }

        if (reservationDateTo.HasValue)
        {
            query = query.Where(reservation => reservation.ReservationDate <= reservationDateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reservation => reservation.ReservationDate)
            .ThenBy(reservation => reservation.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByCustomerAsync(
        int customerId,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        ListFilteredAsync(
            query => query.Where(reservation => reservation.CustomerId == customerId),
            skip,
            take,
            cancellationToken);

    public Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListByDepartureAsync(
        int departureId,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        ListFilteredAsync(
            query => query.Where(reservation => reservation.DepartureId == departureId),
            skip,
            take,
            cancellationToken);

    public Task<Reservation?> FindAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Reservations
            .Include(reservation => reservation.Passengers)
            .Include(reservation => reservation.Cancellations)
            .SingleOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

    public Task<Customer?> FindCustomerAsync(int customerId, CancellationToken cancellationToken) =>
        dbContext.Customers.AsNoTracking()
            .SingleOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);

    public Task<Departure?> FindDepartureAsync(int departureId, CancellationToken cancellationToken) =>
        dbContext.Departures
            .Include(departure => departure.Prices)
            .SingleOrDefaultAsync(departure => departure.Id == departureId, cancellationToken);

    public Task<DeparturePrice?> FindDeparturePriceAsync(
        int departureId,
        DeparturePriceType priceType,
        CancellationToken cancellationToken) =>
        dbContext.DeparturePrices.AsNoTracking()
            .OrderBy(price => price.Id)
            .FirstOrDefaultAsync(
                price => price.DepartureId == departureId && price.PriceType == priceType,
                cancellationToken);

    public Task<Passenger?> FindPassengerAsync(int passengerId, CancellationToken cancellationToken) =>
        dbContext.Passengers.AsNoTracking()
            .SingleOrDefaultAsync(passenger => passenger.Id == passengerId, cancellationToken);

    public Task<int> CountActivePassengersForDepartureAsync(
        int departureId,
        CancellationToken cancellationToken) =>
        dbContext.ReservationPassengers
            .Where(passenger =>
                passenger.Reservation.DepartureId == departureId &&
                CapacityStatuses.Contains(passenger.Reservation.Status))
            .CountAsync(cancellationToken);

    public async Task<(IReadOnlyList<ReservationPassenger> Items, int TotalCount)> ListPassengersAsync(
        int reservationId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ReservationPassengers
            .AsNoTracking()
            .Where(passenger => passenger.ReservationId == reservationId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(passenger => passenger.LastName)
            .ThenBy(passenger => passenger.FirstName)
            .ThenBy(passenger => passenger.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<ReservationPassenger?> FindReservationPassengerAsync(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken) =>
        dbContext.ReservationPassengers.SingleOrDefaultAsync(
            passenger => passenger.ReservationId == reservationId && passenger.Id == passengerId,
            cancellationToken);

    public Task<bool> HasTripForReservationPassengerAsync(
        int passengerId,
        CancellationToken cancellationToken) =>
        dbContext.Trips.AnyAsync(
            trip => trip.ReservationPassengerId == passengerId,
            cancellationToken);

    public Task<bool> PassengerDocumentExistsAsync(
        int reservationId,
        ReservationDocumentType documentType,
        string documentNumber,
        int? excludingPassengerId,
        CancellationToken cancellationToken) =>
        dbContext.ReservationPassengers.AnyAsync(
            passenger =>
                passenger.ReservationId == reservationId &&
                passenger.DocumentType == documentType &&
                passenger.DocumentNumber == documentNumber &&
                (!excludingPassengerId.HasValue || passenger.Id != excludingPassengerId.Value),
            cancellationToken);

    public async Task<IReadOnlyList<ReservationPayment>> ListPaymentsAsync(
        int reservationId,
        CancellationToken cancellationToken) =>
        await dbContext.ReservationPayments
            .AsNoTracking()
            .Where(payment => payment.ReservationId == reservationId)
            .OrderByDescending(payment => payment.CreatedAt)
            .ThenBy(payment => payment.Id)
            .ToListAsync(cancellationToken);

    public async Task<decimal> SumPaidPaymentsAsync(
        int reservationId,
        CancellationToken cancellationToken) =>
        await dbContext.ReservationPayments
            .Where(payment =>
                payment.ReservationId == reservationId &&
                payment.Status == ReservationPaymentStatus.Paid)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken)
            ?? 0m;

    public async Task<bool> AddPassengerIfCapacityAsync(
        ReservationPassenger passenger,
        int departureId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        var departure = await dbContext.Departures
            .FromSqlInterpolated(
                $"SELECT * FROM \"Departures\" WHERE \"Id\" = {departureId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Departure {departureId} disappeared during reservation.");
        if (departure.Status != DepartureStatus.Open)
        {
            throw new ReservationConflictException(
                $"Departure {departureId} is no longer open for reservations.");
        }

        var reservation = await dbContext.Reservations
            .FromSqlInterpolated(
                $"SELECT * FROM \"Reservations\" WHERE \"Id\" = {passenger.ReservationId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Reservation {passenger.ReservationId} disappeared during passenger assignment.");
        if (reservation.Status != ReservationStatus.Pending)
        {
            throw new ReservationConflictException(
                "Only pending reservations may have passengers added.");
        }

        if (passenger.Reservation is not null)
        {
            await dbContext.Entry(passenger.Reservation).ReloadAsync(cancellationToken);
        }

        var passengerCount = await dbContext.ReservationPassengers
            .Where(existing => existing.ReservationId == passenger.ReservationId)
            .CountAsync(cancellationToken);
        var departurePassengerCount = await dbContext.ReservationPassengers
            .Where(existing =>
                existing.Reservation.DepartureId == departureId &&
                CapacityStatuses.Contains(existing.Reservation.Status))
            .CountAsync(cancellationToken);
        if (departurePassengerCount >= departure.Capacity)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (passenger.Reservation is { } trackedReservation)
        {
            var reservationSubtotal = await dbContext.ReservationPassengers
                .Where(existing => existing.ReservationId == passenger.ReservationId)
                .SumAsync(existing => (decimal?)existing.UnitPrice, cancellationToken)
                ?? 0m;
            trackedReservation.NumberOfPassengers = passengerCount + 1;
            trackedReservation.Subtotal = reservationSubtotal + passenger.UnitPrice;
            trackedReservation.TotalAmount =
                trackedReservation.Subtotal - trackedReservation.Discount;
            trackedReservation.UpdatedAt = passenger.CreatedAt;
        }

        dbContext.ReservationPassengers.Add(passenger);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsReservationConflict(exception))
        {
            throw new ReservationConflictException(
                "The reservation number or passenger document is already in use.");
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ReservationPassenger>> ListDeparturePassengersAsync(
        int departureId,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        await dbContext.ReservationPassengers
            .AsNoTracking()
            .Include(passenger => passenger.Reservation)
            .Where(passenger =>
                passenger.Reservation.DepartureId == departureId &&
                CapacityStatuses.Contains(passenger.Reservation.Status))
            .OrderBy(passenger => passenger.LastName)
            .ThenBy(passenger => passenger.FirstName)
            .ThenBy(passenger => passenger.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(Reservation reservation) => dbContext.Reservations.Add(reservation);

    public void Add(ReservationCancellation cancellation) =>
        dbContext.ReservationCancellations.Add(cancellation);

    public void Remove(ReservationPassenger passenger) =>
        dbContext.ReservationPassengers.Remove(passenger);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        SaveChangesHandlingConflictsAsync(cancellationToken);

    private async Task<(IReadOnlyList<Reservation> Items, int TotalCount)> ListFilteredAsync(
        Func<IQueryable<Reservation>, IQueryable<Reservation>> filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = filter(dbContext.Reservations.AsNoTracking());
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reservation => reservation.ReservationDate)
            .ThenBy(reservation => reservation.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    private async Task SaveChangesHandlingConflictsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsReservationConflict(exception))
        {
            throw new ReservationConflictException(
                "The reservation number or passenger document is already in use.");
        }
    }

    private static bool IsReservationConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Reservations_ReservationNumber" or
                "IX_ReservationPassengers_Reservation_Document"
        };
}
