using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Contacts;
using ARTravel.Application.DTOs.Contacts;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.UseCases.Contacts;

public sealed class ContactService(IContactStore store) : IContactService
{
    public async Task<PagedCustomers> ListCustomersAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var skip = GetSkip(page, pageSize);
        var result = await store.ListCustomersAsync(
            skip,
            pageSize,
            NormalizeOptional(search),
            isActive,
            cancellationToken);
        return new PagedCustomers(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<CustomerResponse> GetCustomerAsync(
        int id,
        CancellationToken cancellationToken) =>
        ToResponse(await GetCustomerEntityAsync(id, cancellationToken));

    public async Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        ValidateCustomer(command.CustomerType, command.FirstName, command.LastName, command.BusinessName);
        var customer = new Customer
        {
            CustomerType = command.CustomerType,
            FirstName = NormalizeOptional(command.FirstName),
            LastName = NormalizeOptional(command.LastName),
            BusinessName = NormalizeOptional(command.BusinessName),
            DocumentType = NormalizeRequired(command.DocumentType, nameof(command.DocumentType)),
            DocumentNumber = NormalizeRequired(command.DocumentNumber, nameof(command.DocumentNumber)),
            Email = NormalizeOptional(command.Email)?.ToLowerInvariant(),
            PhoneNumber = NormalizeOptional(command.PhoneNumber),
            Address = NormalizeOptional(command.Address),
            City = NormalizeOptional(command.City),
            Country = NormalizeOptional(command.Country),
            Notes = NormalizeOptional(command.Notes),
            IsActive = command.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        store.Add(customer);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(customer);
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(
        int id,
        UpdateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        ValidateCustomer(command.CustomerType, command.FirstName, command.LastName, command.BusinessName);
        var customer = await GetCustomerEntityAsync(id, cancellationToken);
        customer.CustomerType = command.CustomerType;
        customer.FirstName = NormalizeOptional(command.FirstName);
        customer.LastName = NormalizeOptional(command.LastName);
        customer.BusinessName = NormalizeOptional(command.BusinessName);
        customer.DocumentType = NormalizeRequired(command.DocumentType, nameof(command.DocumentType));
        customer.DocumentNumber = NormalizeRequired(command.DocumentNumber, nameof(command.DocumentNumber));
        customer.Email = NormalizeOptional(command.Email)?.ToLowerInvariant();
        customer.PhoneNumber = NormalizeOptional(command.PhoneNumber);
        customer.Address = NormalizeOptional(command.Address);
        customer.City = NormalizeOptional(command.City);
        customer.Country = NormalizeOptional(command.Country);
        customer.Notes = NormalizeOptional(command.Notes);
        customer.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(customer);
    }

    public async Task<CustomerResponse> SetCustomerStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var customer = await GetCustomerEntityAsync(id, cancellationToken);
        customer.IsActive = isActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(customer);
    }

    public async Task<CustomerSummary> GetCustomerSummaryAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var customer = await GetCustomerEntityAsync(id, cancellationToken);
        var reservationCount = await store.CountCustomerReservationsAsync(id, cancellationToken);
        var totalAmount = await store.SumCustomerReservationAmountsAsync(id, cancellationToken);
        return new CustomerSummary(ToResponse(customer), reservationCount, totalAmount);
    }

    public async Task<PagedPassengers> ListPassengersAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var skip = GetSkip(page, pageSize);
        var result = await store.ListPassengersAsync(
            skip,
            pageSize,
            NormalizeOptional(search),
            isActive,
            cancellationToken);
        return new PagedPassengers(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<PassengerResponse> GetPassengerAsync(
        int id,
        CancellationToken cancellationToken) =>
        ToResponse(await GetPassengerEntityAsync(id, cancellationToken));

    public async Task<PassengerResponse> CreatePassengerAsync(
        CreatePassengerCommand command,
        CancellationToken cancellationToken)
    {
        var passenger = new Passenger
        {
            FirstName = NormalizeRequired(command.FirstName, nameof(command.FirstName)),
            LastName = NormalizeRequired(command.LastName, nameof(command.LastName)),
            DocumentType = NormalizeRequired(command.DocumentType, nameof(command.DocumentType)),
            DocumentNumber = NormalizeRequired(command.DocumentNumber, nameof(command.DocumentNumber)),
            BirthDate = command.BirthDate,
            Nationality = NormalizeOptional(command.Nationality),
            Email = NormalizeOptional(command.Email)?.ToLowerInvariant(),
            PhoneNumber = NormalizeOptional(command.PhoneNumber),
            EmergencyContactName = NormalizeOptional(command.EmergencyContactName),
            EmergencyContactPhone = NormalizeOptional(command.EmergencyContactPhone),
            PassportExpirationDate = command.PassportExpirationDate,
            Notes = NormalizeOptional(command.Notes),
            IsActive = command.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        store.Add(passenger);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(passenger);
    }

    public async Task<PassengerResponse> UpdatePassengerAsync(
        int id,
        UpdatePassengerCommand command,
        CancellationToken cancellationToken)
    {
        var passenger = await GetPassengerEntityAsync(id, cancellationToken);
        passenger.FirstName = NormalizeRequired(command.FirstName, nameof(command.FirstName));
        passenger.LastName = NormalizeRequired(command.LastName, nameof(command.LastName));
        passenger.DocumentType = NormalizeRequired(command.DocumentType, nameof(command.DocumentType));
        passenger.DocumentNumber = NormalizeRequired(command.DocumentNumber, nameof(command.DocumentNumber));
        passenger.BirthDate = command.BirthDate;
        passenger.Nationality = NormalizeOptional(command.Nationality);
        passenger.Email = NormalizeOptional(command.Email)?.ToLowerInvariant();
        passenger.PhoneNumber = NormalizeOptional(command.PhoneNumber);
        passenger.EmergencyContactName = NormalizeOptional(command.EmergencyContactName);
        passenger.EmergencyContactPhone = NormalizeOptional(command.EmergencyContactPhone);
        passenger.PassportExpirationDate = command.PassportExpirationDate;
        passenger.Notes = NormalizeOptional(command.Notes);
        passenger.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(passenger);
    }

    public async Task<PassengerResponse> SetPassengerStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var passenger = await GetPassengerEntityAsync(id, cancellationToken);
        passenger.IsActive = isActive;
        passenger.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(passenger);
    }

    public async Task<PagedTrips> GetPassengerTripsAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _ = await GetPassengerEntityAsync(id, cancellationToken);
        var skip = GetSkip(page, pageSize);
        var result = await store.GetPassengerTripsAsync(
            id,
            skip,
            pageSize,
            cancellationToken);
        return new PagedTrips(
            result.Items.Select(trip => new TripResponse(
                trip.Id,
                trip.ReservationId,
                trip.PassengerId,
                trip.DepartureId,
                trip.ReservationPassengerId,
                trip.Origin,
                trip.Destination,
                trip.DepartureAt,
                trip.ArrivalAt,
                trip.Status)).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    private async Task<Customer> GetCustomerEntityAsync(int id, CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindCustomerAsync(id, cancellationToken)
                ?? throw new ContactNotFoundException($"Customer {id} was not found.")
            : throw new ArgumentException("Customer id must be positive.");

    private async Task<Passenger> GetPassengerEntityAsync(int id, CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindPassengerAsync(id, cancellationToken)
                ?? throw new ContactNotFoundException($"Passenger {id} was not found.")
            : throw new ArgumentException("Passenger id must be positive.");

    private static int GetSkip(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100 ||
            ((long)page - 1) * pageSize > int.MaxValue)
        {
            throw new ArgumentException("Page must be positive and pageSize must be between 1 and 100.");
        }

        return checked((page - 1) * pageSize);
    }

    private static void ValidateCustomer(
        CustomerType customerType,
        string? firstName,
        string? lastName,
        string? businessName)
    {
        if (!Enum.IsDefined(customerType))
        {
            throw new ArgumentException("CustomerType must be 1 (Individual) or 2 (Organization).");
        }

        if (customerType == CustomerType.Individual &&
            (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)))
        {
            throw new ArgumentException("FirstName and LastName are required for an individual customer.");
        }

        if (customerType == CustomerType.Organization && string.IsNullOrWhiteSpace(businessName))
        {
            throw new ArgumentException("BusinessName is required for an organization customer.");
        }
    }

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

    private static CustomerResponse ToResponse(Customer customer) =>
        new(
            customer.Id,
            customer.CustomerType,
            customer.FirstName,
            customer.LastName,
            customer.BusinessName,
            customer.DocumentType,
            customer.DocumentNumber,
            customer.Email,
            customer.PhoneNumber,
            customer.Address,
            customer.City,
            customer.Country,
            customer.Notes,
            customer.IsActive,
            customer.CreatedAt,
            customer.UpdatedAt);

    private static PassengerResponse ToResponse(Passenger passenger) =>
        new(
            passenger.Id,
            passenger.FirstName,
            passenger.LastName,
            passenger.DocumentType,
            passenger.DocumentNumber,
            passenger.BirthDate,
            passenger.Nationality,
            passenger.Email,
            passenger.PhoneNumber,
            passenger.EmergencyContactName,
            passenger.EmergencyContactPhone,
            passenger.PassportExpirationDate,
            passenger.Notes,
            passenger.IsActive,
            passenger.CreatedAt,
            passenger.UpdatedAt);
}
