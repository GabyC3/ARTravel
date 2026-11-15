using ARTravel.Application.DTOs.Contacts;

namespace ARTravel.Application.Interfaces.Contacts;

public interface IContactService
{
    Task<PagedCustomers> ListCustomersAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<CustomerResponse> GetCustomerAsync(int id, CancellationToken cancellationToken);

    Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken);

    Task<CustomerResponse> UpdateCustomerAsync(
        int id,
        UpdateCustomerCommand command,
        CancellationToken cancellationToken);

    Task<CustomerResponse> SetCustomerStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken);

    Task<CustomerSummary> GetCustomerSummaryAsync(int id, CancellationToken cancellationToken);

    Task<PagedPassengers> ListPassengersAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<PassengerResponse> GetPassengerAsync(int id, CancellationToken cancellationToken);

    Task<PassengerResponse> CreatePassengerAsync(
        CreatePassengerCommand command,
        CancellationToken cancellationToken);

    Task<PassengerResponse> UpdatePassengerAsync(
        int id,
        UpdatePassengerCommand command,
        CancellationToken cancellationToken);

    Task<PassengerResponse> SetPassengerStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken);

    Task<PagedTrips> GetPassengerTripsAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
