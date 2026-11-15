using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Reservations;
using ARTravel.Application.DTOs.Reservations;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.UseCases.Reservations;

public sealed class ReservationService(IReservationStore store) : IReservationService
{
    public async Task<PagedReservations> ListAsync(
        int page,
        int pageSize,
        string? search,
        int? customerId,
        int? departureId,
        ReservationStatus? status,
        DateTime? reservationDateFrom,
        DateTime? reservationDateTo,
        CancellationToken cancellationToken)
    {
        var skip = GetSkip(page, pageSize);
        ValidateStatus(status);
        ValidateOptionalId(customerId, nameof(customerId));
        ValidateOptionalId(departureId, nameof(departureId));
        ValidateDateFilters(reservationDateFrom, reservationDateTo);
        var result = await store.ListAsync(
            skip,
            pageSize,
            NormalizeOptional(search),
            customerId,
            departureId,
            status,
            reservationDateFrom,
            reservationDateTo,
            cancellationToken);
        return new PagedReservations(
            result.Items.Select(ToListItem).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<ReservationDetail> GetAsync(int id, CancellationToken cancellationToken) =>
        ToDetail(await GetReservationAsync(id, cancellationToken));

    public async Task<ReservationDetail> CreateAsync(
        CreateReservationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CreatedByUserId <= 0)
        {
            throw new ArgumentException("CreatedByUserId must be positive.");
        }

        var customer = await store.FindCustomerAsync(command.CustomerId, cancellationToken)
            ?? throw new ContactNotFoundException($"Customer {command.CustomerId} was not found.");
        if (!customer.IsActive)
        {
            throw new ReservationConflictException("An inactive customer cannot create a reservation.");
        }

        var departure = await GetDepartureAsync(command.DepartureId, cancellationToken);
        EnsureDepartureOpen(departure);
        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            ReservationNumber = $"RES-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
            CustomerId = customer.Id,
            DepartureId = departure.Id,
            ReservationDate = now,
            NumberOfPassengers = 0,
            Subtotal = 0m,
            Discount = 0m,
            TotalAmount = 0m,
            Currency = departure.Currency,
            Status = ReservationStatus.Pending,
            Notes = NormalizeOptional(command.Notes),
            CreatedByUserId = command.CreatedByUserId,
            CreatedAt = now
        };
        store.Add(reservation);
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(reservation.Id, cancellationToken);
    }

    public async Task<ReservationDetail> UpdateAsync(
        int id,
        UpdateReservationCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await GetReservationAsync(id, cancellationToken);
        EnsurePending(reservation);
        ValidateDiscount(command.Discount, reservation.Subtotal);
        reservation.Notes = NormalizeOptional(command.Notes);
        reservation.Discount = command.Discount;
        reservation.TotalAmount = reservation.Subtotal - reservation.Discount;
        reservation.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToDetail(reservation);
    }

    public Task<ReservationDetail> ConfirmAsync(int id, CancellationToken cancellationToken) =>
        SetStatusAsync(
            id,
            new SetReservationStatusCommand(ReservationStatus.Confirmed),
            cancellationToken);

    public async Task<ReservationDetail> CancelAsync(
        int id,
        CancelReservationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CancelledByUserId <= 0)
        {
            throw new ArgumentException("CancelledByUserId must be positive.");
        }

        var reservation = await GetReservationAsync(id, cancellationToken);
        if (reservation.Status is not (
            ReservationStatus.Pending or
            ReservationStatus.Confirmed or
            ReservationStatus.PartiallyPaid or
            ReservationStatus.Paid))
        {
            throw new ReservationConflictException(
                $"A reservation in status {reservation.Status} cannot be cancelled.");
        }

        var reason = NormalizeRequired(command.Reason, nameof(command.Reason));
        if (command.RefundAmount < 0)
        {
            throw new ArgumentException("RefundAmount cannot be negative.");
        }

        var paidAmount = await store.SumPaidPaymentsAsync(id, cancellationToken);
        if (command.RefundAmount > paidAmount)
        {
            throw new ReservationConflictException(
                "RefundAmount cannot exceed the total amount of paid payments.");
        }

        var now = DateTime.UtcNow;
        store.Add(new ReservationCancellation
        {
            ReservationId = id,
            Reason = reason,
            CancelledAt = now,
            CancelledByUserId = command.CancelledByUserId,
            RefundAmount = command.RefundAmount,
            Reservation = reservation
        });
        reservation.Status = ReservationStatus.Cancelled;
        reservation.UpdatedAt = now;
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<ReservationDetail> SetStatusAsync(
        int id,
        SetReservationStatusCommand command,
        CancellationToken cancellationToken)
    {
        ValidateStatus(command.Status);
        var reservation = await GetReservationAsync(id, cancellationToken);
        if (!IsAllowedTransition(reservation.Status, command.Status))
        {
            throw new ReservationConflictException(
                $"The transition from {reservation.Status} to {command.Status} is not allowed.");
        }

        if (command.Status == ReservationStatus.Confirmed)
        {
            var departure = await GetDepartureAsync(reservation.DepartureId, cancellationToken);
            EnsureDepartureOpen(departure);
            var passengerCount = await store.CountActivePassengersForDepartureAsync(
                departure.Id,
                cancellationToken);
            if (passengerCount < departure.MinimumPassengers)
            {
                throw new ReservationConflictException(
                    $"Departure {departure.Id} requires at least {departure.MinimumPassengers} passengers before confirmation.");
            }
        }

        reservation.Status = command.Status;
        reservation.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToDetail(reservation);
    }

    public async Task<PagedReservations> ListByCustomerAsync(
        int customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _ = await GetCustomerAsync(customerId, cancellationToken);
        var skip = GetSkip(page, pageSize);
        var result = await store.ListByCustomerAsync(customerId, skip, pageSize, cancellationToken);
        return new PagedReservations(
            result.Items.Select(ToListItem).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<PagedReservations> ListByDepartureAsync(
        int departureId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _ = await GetDepartureAsync(departureId, cancellationToken);
        var skip = GetSkip(page, pageSize);
        var result = await store.ListByDepartureAsync(departureId, skip, pageSize, cancellationToken);
        return new PagedReservations(
            result.Items.Select(ToListItem).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<IReadOnlyList<ReservationPaymentResponse>> GetPaymentsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        _ = await GetReservationAsync(id, cancellationToken);
        return (await store.ListPaymentsAsync(id, cancellationToken))
            .Select(ToResponse)
            .ToArray();
    }

    public async Task<(IReadOnlyList<ReservationPassengerResponse> Items, int Page, int PageSize, int TotalCount)>
        ListPassengersAsync(
            int reservationId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        _ = await GetReservationAsync(reservationId, cancellationToken);
        var skip = GetSkip(page, pageSize);
        var result = await store.ListPassengersAsync(
            reservationId,
            skip,
            pageSize,
            cancellationToken);
        return (
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<ReservationPassengerResponse> GetPassengerAsync(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken)
    {
        _ = await GetReservationAsync(reservationId, cancellationToken);
        if (passengerId <= 0)
        {
            throw new ArgumentException("Passenger id must be positive.");
        }

        var passenger = await store.FindReservationPassengerAsync(
            reservationId,
            passengerId,
            cancellationToken);
        return passenger is null
            ? throw new ReservationNotFoundException(
                $"Passenger {passengerId} was not found for reservation {reservationId}.")
            : ToResponse(passenger);
    }

    public async Task<ReservationPassengerResponse> AddPassengerAsync(
        int reservationId,
        CreateReservationPassengerCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await GetReservationAsync(reservationId, cancellationToken);
        EnsurePending(reservation);
        var departure = await GetDepartureAsync(reservation.DepartureId, cancellationToken);
        EnsureDepartureOpen(departure);
        ValidatePassenger(command);
        await EnsureGlobalPassengerExistsAsync(
            command.PassengerId,
            command.DocumentType,
            command.DocumentNumber,
            cancellationToken);
        var documentNumber = NormalizeDocumentNumber(command.DocumentNumber);
        if (await store.PassengerDocumentExistsAsync(
                reservationId,
                command.DocumentType,
                documentNumber,
                null,
                cancellationToken))
        {
            throw new ReservationConflictException(
                "A passenger with this document is already assigned to the reservation.");
        }

        var unitPrice = await GetUnitPriceAsync(
            departure,
            command.PriceType,
            cancellationToken);
        var passenger = new ReservationPassenger
        {
            ReservationId = reservationId,
            PassengerId = command.PassengerId,
            FirstName = NormalizeRequired(command.FirstName, nameof(command.FirstName)),
            LastName = NormalizeRequired(command.LastName, nameof(command.LastName)),
            DocumentType = command.DocumentType,
            DocumentNumber = documentNumber,
            BirthDate = command.BirthDate,
            Gender = NormalizeOptional(command.Gender),
            PhoneNumber = NormalizeOptional(command.PhoneNumber),
            Email = NormalizeEmail(command.Email),
            EmergencyContactName = NormalizeOptional(command.EmergencyContactName),
            EmergencyContactPhone = NormalizeOptional(command.EmergencyContactPhone),
            SpecialRequirements = NormalizeOptional(command.SpecialRequirements),
            Notes = NormalizeOptional(command.Notes),
            PriceType = command.PriceType,
            UnitPrice = unitPrice.Amount,
            Currency = unitPrice.Currency,
            CreatedAt = DateTime.UtcNow,
            Reservation = reservation
        };

        if (!await store.AddPassengerIfCapacityAsync(
                passenger,
                departure.Id,
                cancellationToken))
        {
            throw new ReservationConflictException(
                $"Departure {departure.Id} has no remaining capacity.");
        }

        return ToResponse(passenger);
    }

    public async Task<ReservationPassengerResponse> UpdatePassengerAsync(
        int reservationId,
        int passengerId,
        UpdateReservationPassengerCommand command,
        CancellationToken cancellationToken)
    {
        var reservation = await GetReservationAsync(reservationId, cancellationToken);
        EnsurePending(reservation);
        if (passengerId <= 0)
        {
            throw new ArgumentException("Passenger id must be positive.");
        }

        ValidatePassenger(command);
        await EnsureGlobalPassengerExistsAsync(
            command.PassengerId,
            command.DocumentType,
            command.DocumentNumber,
            cancellationToken);
        var passenger = await store.FindReservationPassengerAsync(
            reservationId,
            passengerId,
            cancellationToken)
            ?? throw new ReservationNotFoundException(
                $"Passenger {passengerId} was not found for reservation {reservationId}.");
        var documentNumber = NormalizeDocumentNumber(command.DocumentNumber);
        if (await store.PassengerDocumentExistsAsync(
                reservationId,
                command.DocumentType,
                documentNumber,
                passengerId,
                cancellationToken))
        {
            throw new ReservationConflictException(
                "A passenger with this document is already assigned to the reservation.");
        }

        var departure = await GetDepartureAsync(reservation.DepartureId, cancellationToken);
        var unitPrice = await GetUnitPriceAsync(departure, command.PriceType, cancellationToken);
        passenger.PassengerId = command.PassengerId;
        passenger.FirstName = NormalizeRequired(command.FirstName, nameof(command.FirstName));
        passenger.LastName = NormalizeRequired(command.LastName, nameof(command.LastName));
        passenger.DocumentType = command.DocumentType;
        passenger.DocumentNumber = documentNumber;
        passenger.BirthDate = command.BirthDate;
        passenger.Gender = NormalizeOptional(command.Gender);
        passenger.PhoneNumber = NormalizeOptional(command.PhoneNumber);
        passenger.Email = NormalizeEmail(command.Email);
        passenger.EmergencyContactName = NormalizeOptional(command.EmergencyContactName);
        passenger.EmergencyContactPhone = NormalizeOptional(command.EmergencyContactPhone);
        passenger.SpecialRequirements = NormalizeOptional(command.SpecialRequirements);
        passenger.Notes = NormalizeOptional(command.Notes);
        passenger.PriceType = command.PriceType;
        passenger.UnitPrice = unitPrice.Amount;
        passenger.Currency = unitPrice.Currency;
        passenger.UpdatedAt = DateTime.UtcNow;
        RecalculateAmounts(reservation);
        reservation.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(passenger);
    }

    public async Task RemovePassengerAsync(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken)
    {
        var reservation = await GetReservationAsync(reservationId, cancellationToken);
        EnsurePending(reservation);
        if (passengerId <= 0)
        {
            throw new ArgumentException("Passenger id must be positive.");
        }

        var passenger = await store.FindReservationPassengerAsync(
            reservationId,
            passengerId,
            cancellationToken)
            ?? throw new ReservationNotFoundException(
                $"Passenger {passengerId} was not found for reservation {reservationId}.");
        if (await store.HasTripForReservationPassengerAsync(passengerId, cancellationToken))
        {
            throw new ReservationConflictException(
                "A passenger linked to a Trip cannot be removed from the reservation.");
        }

        reservation.Passengers.Remove(passenger);
        store.Remove(passenger);
        reservation.NumberOfPassengers = reservation.Passengers.Count;
        RecalculateAmounts(reservation);
        reservation.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedReservationPassengers> ListDeparturePassengersAsync(
            int departureId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        _ = await GetDepartureAsync(departureId, cancellationToken);
        var skip = GetSkip(page, pageSize);
        var items = await store.ListDeparturePassengersAsync(
            departureId,
            skip,
            pageSize,
            cancellationToken);
        var totalCount = await store.CountActivePassengersForDepartureAsync(
            departureId,
            cancellationToken);
        return new PagedReservationPassengers(
            items.Select(ToDeparturePassenger).ToArray(),
            page,
            pageSize,
            totalCount);
    }

    private async Task<Reservation> GetReservationAsync(
        int id,
        CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindAsync(id, cancellationToken)
                ?? throw new ReservationNotFoundException($"Reservation {id} was not found.")
            : throw new ArgumentException("Reservation id must be positive.");

    private async Task<Customer> GetCustomerAsync(
        int id,
        CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindCustomerAsync(id, cancellationToken)
                ?? throw new ContactNotFoundException($"Customer {id} was not found.")
            : throw new ArgumentException("Customer id must be positive.");

    private async Task<Departure> GetDepartureAsync(
        int id,
        CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindDepartureAsync(id, cancellationToken)
                ?? throw new DepartureNotFoundException($"Departure {id} was not found.")
            : throw new ArgumentException("Departure id must be positive.");

    private async Task EnsureGlobalPassengerExistsAsync(
        int? passengerId,
        ReservationDocumentType documentType,
        string documentNumber,
        CancellationToken cancellationToken)
    {
        if (!passengerId.HasValue)
        {
            return;
        }

        if (passengerId <= 0)
        {
            throw new ArgumentException("PassengerId must be positive.");
        }

        var passenger = await store.FindPassengerAsync(passengerId.Value, cancellationToken)
            ?? throw new ContactNotFoundException($"Passenger {passengerId} was not found.");
        var expectedDocumentType = documentType.ToString();
        if (!string.Equals(
                passenger.DocumentType,
                expectedDocumentType,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                passenger.DocumentNumber.Trim(),
                NormalizeDocumentNumber(documentNumber),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "PassengerId must reference a profile with the same document type and number.");
        }
    }

    private async Task<(decimal Amount, string Currency)> GetUnitPriceAsync(
        Departure departure,
        DeparturePriceType priceType,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(priceType))
        {
            throw new ArgumentException("PriceType must be between 1 (Standard) and 4 (Promotional).");
        }

        var price = await store.FindDeparturePriceAsync(
            departure.Id,
            priceType,
            cancellationToken);
        if (price is null)
        {
            if (priceType == DeparturePriceType.Standard)
            {
                return (departure.BasePrice, departure.Currency);
            }

            throw new ArgumentException(
                $"Departure {departure.Id} has no configured {priceType} price.");
        }

        if (!string.Equals(price.Currency, departure.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new ReservationConflictException(
                "The passenger price currency must match the departure currency.");
        }

        return (price.Amount, price.Currency);
    }

    private static bool IsAllowedTransition(
        ReservationStatus current,
        ReservationStatus requested) =>
        (current, requested) switch
        {
            (ReservationStatus.Pending, ReservationStatus.Confirmed) => true,
            (ReservationStatus.Pending, ReservationStatus.Expired) => true,
            (ReservationStatus.Confirmed, ReservationStatus.PartiallyPaid) => true,
            (ReservationStatus.Confirmed, ReservationStatus.Paid) => true,
            (ReservationStatus.PartiallyPaid, ReservationStatus.Paid) => true,
            (ReservationStatus.Paid, ReservationStatus.Completed) => true,
            _ => false
        };

    private static void EnsureDepartureOpen(Departure departure)
    {
        if (departure.Status != DepartureStatus.Open)
        {
            throw new ReservationConflictException(
                $"Departure {departure.Id} is not open for reservations.");
        }
    }

    private static void EnsurePending(Reservation reservation)
    {
        if (reservation.Status != ReservationStatus.Pending)
        {
            throw new ReservationConflictException(
                "Only pending reservations may be edited.");
        }
    }

    private static void ValidatePassenger(CreateReservationPassengerCommand command)
    {
        ValidateDocumentType(command.DocumentType);
        _ = NormalizeRequired(command.FirstName, nameof(command.FirstName));
        _ = NormalizeRequired(command.LastName, nameof(command.LastName));
        _ = NormalizeDocumentNumber(command.DocumentNumber);
        if (command.PassengerId is <= 0)
        {
            throw new ArgumentException("PassengerId must be positive when supplied.");
        }
    }

    private static void ValidatePassenger(UpdateReservationPassengerCommand command)
    {
        ValidateDocumentType(command.DocumentType);
        _ = NormalizeRequired(command.FirstName, nameof(command.FirstName));
        _ = NormalizeRequired(command.LastName, nameof(command.LastName));
        _ = NormalizeDocumentNumber(command.DocumentNumber);
        if (command.PassengerId is <= 0)
        {
            throw new ArgumentException("PassengerId must be positive when supplied.");
        }
    }

    private static void ValidateDocumentType(ReservationDocumentType documentType)
    {
        if (!Enum.IsDefined(documentType))
        {
            throw new ArgumentException("DocumentType must be 1 (DNI), 2 (Passport), or 3 (Other).");
        }
    }

    private static void ValidateStatus(ReservationStatus? status)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentException("Status must be a defined ReservationStatus value from 1 to 7.");
        }
    }

    private static void ValidateDateFilters(DateTime? from, DateTime? to)
    {
        if (from.HasValue && from.Value.Kind != DateTimeKind.Utc ||
            to.HasValue && to.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Reservation date filters must be UTC.");
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ArgumentException("ReservationDateFrom cannot be later than ReservationDateTo.");
        }
    }

    private static void ValidateOptionalId(int? id, string name)
    {
        if (id is <= 0)
        {
            throw new ArgumentException($"{name} must be positive.");
        }
    }

    private static int GetSkip(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 ||
            ((long)page - 1) * pageSize > int.MaxValue)
        {
            throw new ArgumentException("Page must be positive and pageSize must be between 1 and 100.");
        }

        return checked((page - 1) * pageSize);
    }

    private static void ValidateDiscount(decimal discount, decimal subtotal)
    {
        if (discount < 0 || discount > subtotal)
        {
            throw new ArgumentException("Discount must be between zero and Subtotal.");
        }
    }

    private static void RecalculateAmounts(Reservation reservation)
    {
        reservation.NumberOfPassengers = reservation.Passengers.Count;
        reservation.Subtotal = reservation.Passengers.Sum(passenger => passenger.UnitPrice);
        ValidateDiscount(reservation.Discount, reservation.Subtotal);
        reservation.TotalAmount = reservation.Subtotal - reservation.Discount;
    }

    private static string NormalizeDocumentNumber(string? documentNumber) =>
        NormalizeRequired(documentNumber, nameof(documentNumber)).ToUpperInvariant();

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmail(string? email) =>
        NormalizeOptional(email)?.ToLowerInvariant();

    private static ReservationDetail ToDetail(Reservation reservation) =>
        new(
            ToListItem(reservation),
            reservation.Passengers
                .OrderBy(passenger => passenger.Id)
                .Select(ToResponse)
                .ToArray(),
            reservation.Cancellations
                .OrderByDescending(cancellation => cancellation.CancelledAt)
                .Select(ToResponse)
                .FirstOrDefault());

    private static ReservationListItem ToListItem(Reservation reservation) =>
        new(
            reservation.Id,
            reservation.ReservationNumber,
            reservation.CustomerId,
            reservation.DepartureId,
            reservation.ReservationDate,
            reservation.NumberOfPassengers,
            reservation.Subtotal,
            reservation.Discount,
            reservation.TotalAmount,
            reservation.Currency,
            reservation.Status,
            reservation.Notes,
            reservation.CreatedByUserId,
            reservation.CreatedAt,
            reservation.UpdatedAt);

    private static ReservationPassengerResponse ToResponse(ReservationPassenger passenger) =>
        new(
            passenger.Id,
            passenger.ReservationId,
            passenger.PassengerId,
            passenger.FirstName,
            passenger.LastName,
            passenger.DocumentType,
            passenger.DocumentNumber,
            passenger.BirthDate,
            passenger.Gender,
            passenger.PhoneNumber,
            passenger.Email,
            passenger.EmergencyContactName,
            passenger.EmergencyContactPhone,
            passenger.SpecialRequirements,
            passenger.Notes,
            passenger.PriceType,
            passenger.UnitPrice,
            passenger.Currency,
            passenger.CreatedAt,
            passenger.UpdatedAt);

    private static ReservationCancellationResponse ToResponse(
        ReservationCancellation cancellation) =>
        new(
            cancellation.Id,
            cancellation.ReservationId,
            cancellation.Reason,
            cancellation.CancelledAt,
            cancellation.CancelledByUserId,
            cancellation.RefundAmount);

    private static ReservationPaymentResponse ToResponse(ReservationPayment payment) =>
        new(
            payment.Id,
            payment.ReservationId,
            payment.Amount,
            payment.Currency,
            payment.PaymentMethod,
            payment.Status,
            payment.TransactionReference,
            payment.PaidAt,
            payment.Notes,
            payment.CreatedAt);

    private static ReservationPassengerListItem ToDeparturePassenger(
        ReservationPassenger passenger) =>
        new(
            passenger.ReservationId,
            passenger.Reservation.ReservationNumber,
            passenger.Id,
            passenger.PassengerId,
            passenger.FirstName,
            passenger.LastName,
            passenger.DocumentType,
            passenger.DocumentNumber,
            passenger.BirthDate,
            passenger.Gender,
            passenger.PhoneNumber,
            passenger.Email,
            passenger.EmergencyContactName,
            passenger.EmergencyContactPhone,
            passenger.SpecialRequirements,
            passenger.Notes);
}
