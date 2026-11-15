using ARTravel.Application.DTOs.Departures;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Departures;

public interface IDepartureService
{
    Task<PagedDepartures> ListAsync(
        int page,
        int pageSize,
        int? productId,
        DepartureStatus? status,
        CancellationToken cancellationToken);

    Task<DepartureResponse> GetAsync(int id, CancellationToken cancellationToken);

    Task<DepartureResponse> CreateAsync(
        CreateDepartureCommand command,
        CancellationToken cancellationToken);

    Task<DepartureResponse> CreateForProductAsync(
        int productId,
        CreateDepartureCommand command,
        CancellationToken cancellationToken);

    Task<DepartureResponse> UpdateAsync(
        int id,
        UpdateDepartureCommand command,
        CancellationToken cancellationToken);

    Task<DepartureResponse> SetStatusAsync(
        int id,
        DepartureStatus status,
        CancellationToken cancellationToken);

    Task<DepartureResponse> CancelAsync(int id, CancellationToken cancellationToken);

    Task<PagedDepartures> GetProductDeparturesAsync(
        int productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DepartureAvailability> GetAvailabilityAsync(
        int id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DeparturePriceResponse>> GetPricesAsync(
        int id,
        CancellationToken cancellationToken);

    Task<DeparturePriceResponse> AddPriceAsync(
        int id,
        CreateDeparturePriceCommand command,
        CancellationToken cancellationToken);

    Task<DeparturePriceResponse> UpdatePriceAsync(
        int id,
        int priceId,
        CreateDeparturePriceCommand command,
        CancellationToken cancellationToken);

    Task RemovePriceAsync(int id, int priceId, CancellationToken cancellationToken);
}
