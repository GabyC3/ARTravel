using ARTravel.Application.DTOs.Destinations;
using ARTravel.Application.DTOs.Products;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Destinations;

public interface IDestinationService
{
    Task<PagedDestinations> ListAsync(
        int page,
        int pageSize,
        string? search,
        DestinationType? destinationType,
        string? country,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<DestinationDetail> GetAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DestinationResponse>> GetParentDestinationsAsync(
        CancellationToken cancellationToken);

    Task<DestinationDetail> CreateAsync(
        CreateDestinationCommand command,
        CancellationToken cancellationToken);

    Task<DestinationDetail> UpdateAsync(
        int id,
        UpdateDestinationCommand command,
        CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task<DestinationResponse> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken);

    Task<PagedDestinations> GetChildrenAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedProducts> GetProductsAsync(
        int id,
        int page,
        int pageSize,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<DestinationSummary> GetSummaryAsync(int id, CancellationToken cancellationToken);

    Task<DestinationImageResponse> AddImageAsync(
        int id,
        AddDestinationImageCommand command,
        CancellationToken cancellationToken);

    Task RemoveImageAsync(int id, int imageId, CancellationToken cancellationToken);

    Task<DestinationImageResponse> SetPrimaryImageAsync(
        int id,
        int imageId,
        CancellationToken cancellationToken);
}
