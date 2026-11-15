using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Products;

public interface IProductStore
{
    Task<(IReadOnlyList<TourismProduct> Items, int TotalCount)> ListProductsAsync(
        int skip,
        int take,
        string? search,
        ProductType? productType,
        bool? isPublished,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<TourismProduct?> FindProductAsync(int id, CancellationToken cancellationToken);

    Task<TourismProduct?> FindProductByCodeAsync(
        string code,
        int? excludingProductId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductDestination>> GetProductDestinationsAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> FindMissingDestinationIdsAsync(
        IReadOnlyCollection<int> destinationIds,
        CancellationToken cancellationToken);

    Task ReplaceProductDestinationsAsync(
        int productId,
        IReadOnlyList<ProductDestination> destinations,
        CancellationToken cancellationToken);

    Task<bool> RemoveProductDestinationAsync(
        int productId,
        int destinationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TourismProductImage>> GetProductImagesAsync(
        int productId,
        CancellationToken cancellationToken);

    Task<TourismProductImage?> FindProductImageAsync(int imageId, CancellationToken cancellationToken);

    void Add(TourismProduct product);

    void Add(TourismProductImage image);

    void Remove(TourismProductImage image);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
