using ARTravel.Application.DTOs.Products;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Products;

public interface IProductService
{
    Task<PagedProducts> ListAsync(
        int page,
        int pageSize,
        string? search,
        ProductType? productType,
        bool? isPublished,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<ProductDetail> GetAsync(int id, CancellationToken cancellationToken);

    Task<ProductDetail> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken);

    Task<ProductDetail> UpdateAsync(
        int id,
        UpdateProductCommand command,
        CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task<TourismProductResponse> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken);

    Task<TourismProductResponse> SetPublicationAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductDestinationResponse>> GetDestinationsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductDestinationResponse>> AssignDestinationsAsync(
        int id,
        IReadOnlyList<AssignProductDestinationItem> destinations,
        CancellationToken cancellationToken);

    Task RemoveDestinationAsync(int id, int destinationId, CancellationToken cancellationToken);

    Task<ProductImageResponse> AddImageAsync(
        int id,
        string imageUrl,
        string? altText,
        bool isPrimary,
        int displayOrder,
        CancellationToken cancellationToken);

    Task RemoveImageAsync(int id, int imageId, CancellationToken cancellationToken);

    Task<ProductImageResponse> SetPrimaryImageAsync(
        int id,
        int imageId,
        CancellationToken cancellationToken);

}
