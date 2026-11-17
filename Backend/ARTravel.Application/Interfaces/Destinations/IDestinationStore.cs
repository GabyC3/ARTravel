using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Destinations;

public interface IDestinationStore
{
    Task<(IReadOnlyList<Destination> Items, int TotalCount)> ListDestinationsAsync(
        int skip,
        int take,
        string? search,
        DestinationType? destinationType,
        string? country,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<Destination?> FindDestinationAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Destination>> ListParentDestinationsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, string>> GetPrimaryImageUrlsAsync(
        IReadOnlyCollection<int> destinationIds,
        CancellationToken cancellationToken);

    Task<bool> HasReferencesAsync(int destinationId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Destination> Items, int TotalCount)> ListChildDestinationsAsync(
        int parentId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<TourismProduct> Items, int TotalCount)> ListDestinationProductsAsync(
        int destinationId,
        int skip,
        int take,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<int> CountChildDestinationsAsync(int destinationId, CancellationToken cancellationToken);

    Task<int> CountDestinationProductsAsync(int destinationId, CancellationToken cancellationToken);

    Task<int> CountDestinationImagesAsync(int destinationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DestinationImage>> GetDestinationImagesAsync(
        int destinationId,
        CancellationToken cancellationToken);

    Task<DestinationImage?> FindDestinationImageAsync(int imageId, CancellationToken cancellationToken);

    void Add(Destination destination);

    void Remove(Destination destination);

    void Add(DestinationImage image);

    void Remove(DestinationImage image);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
