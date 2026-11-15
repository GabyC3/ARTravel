using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Products;
using ARTravel.Application.DTOs.Products;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.UseCases.Products;

public sealed class ProductService(IProductStore store) : IProductService
{
    public async Task<PagedProducts> ListAsync(
        int page,
        int pageSize,
        string? search,
        ProductType? productType,
        bool? isPublished,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var skip = GetSkip(page, pageSize);
        ValidateProductType(productType);
        var result = await store.ListProductsAsync(
            skip,
            pageSize,
            NormalizeOptional(search),
            productType,
            isPublished,
            isActive,
            cancellationToken);
        return new PagedProducts(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<ProductDetail> GetAsync(int id, CancellationToken cancellationToken)
    {
        var product = await GetProductAsync(id, cancellationToken);
        var destinations = await store.GetProductDestinationsAsync(id, cancellationToken);
        var images = await store.GetProductImagesAsync(id, cancellationToken);
        return new ProductDetail(
            ToResponse(product),
            destinations.Select(ToResponse).ToArray(),
            images.Select(ToResponse).ToArray());
    }

    public async Task<ProductDetail> CreateAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        ValidateProduct(
            command.ProductType,
            command.DurationDays,
            command.DurationNights,
            command.BasePrice,
            command.Currency,
            command.MinimumPassengers,
            command.MaximumPassengers);
        var code = NormalizeCode(command.Code);
        await EnsureCodeAvailableAsync(code, null, cancellationToken);

        var product = new TourismProduct
        {
            Name = NormalizeRequired(command.Name, nameof(command.Name)),
            Code = code,
            Description = NormalizeOptional(command.Description),
            ProductType = command.ProductType,
            DurationDays = command.DurationDays,
            DurationNights = command.DurationNights,
            BasePrice = command.BasePrice,
            Currency = NormalizeCurrency(command.Currency),
            MinimumPassengers = command.MinimumPassengers,
            MaximumPassengers = command.MaximumPassengers,
            Includes = NormalizeOptional(command.Includes),
            Excludes = NormalizeOptional(command.Excludes),
            Recommendations = NormalizeOptional(command.Recommendations),
            CancellationPolicy = NormalizeOptional(command.CancellationPolicy),
            IsPublished = command.IsPublished,
            IsActive = command.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        store.Add(product);
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(product.Id, cancellationToken);
    }

    public async Task<ProductDetail> UpdateAsync(
        int id,
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        ValidateProduct(
            command.ProductType,
            command.DurationDays,
            command.DurationNights,
            command.BasePrice,
            command.Currency,
            command.MinimumPassengers,
            command.MaximumPassengers);
        var product = await GetProductAsync(id, cancellationToken);
        var code = NormalizeCode(command.Code);
        await EnsureCodeAvailableAsync(code, id, cancellationToken);

        product.Name = NormalizeRequired(command.Name, nameof(command.Name));
        product.Code = code;
        product.Description = NormalizeOptional(command.Description);
        product.ProductType = command.ProductType;
        product.DurationDays = command.DurationDays;
        product.DurationNights = command.DurationNights;
        product.BasePrice = command.BasePrice;
        product.Currency = NormalizeCurrency(command.Currency);
        product.MinimumPassengers = command.MinimumPassengers;
        product.MaximumPassengers = command.MaximumPassengers;
        product.Includes = NormalizeOptional(command.Includes);
        product.Excludes = NormalizeOptional(command.Excludes);
        product.Recommendations = NormalizeOptional(command.Recommendations);
        product.CancellationPolicy = NormalizeOptional(command.CancellationPolicy);
        product.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<TourismProductResponse> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var product = await GetProductAsync(id, cancellationToken);
        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<TourismProductResponse> SetPublicationAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken)
    {
        var product = await GetProductAsync(id, cancellationToken);
        product.IsPublished = isPublished;
        product.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(product);
    }

    public async Task<IReadOnlyList<ProductDestinationResponse>> GetDestinationsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        _ = await GetProductAsync(id, cancellationToken);
        var destinations = await store.GetProductDestinationsAsync(id, cancellationToken);
        return destinations.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<ProductDestinationResponse>> AssignDestinationsAsync(
        int id,
        IReadOnlyList<AssignProductDestinationItem> destinations,
        CancellationToken cancellationToken)
    {
        _ = await GetProductAsync(id, cancellationToken);
        if (destinations.Any(item => item.DestinationId <= 0 || item.VisitOrder < 1))
        {
            throw new ArgumentException("Destination ids and VisitOrder must be positive.");
        }

        if (destinations.Select(item => item.DestinationId).Distinct().Count() != destinations.Count)
        {
            throw new ArgumentException("A destination cannot be assigned more than once.");
        }

        if (destinations.Select(item => item.VisitOrder).Distinct().Count() != destinations.Count)
        {
            throw new ArgumentException("VisitOrder values must be unique within a product.");
        }

        if (destinations.Count(item => item.IsMainDestination) > 1)
        {
            throw new ArgumentException("Only one destination can be the main destination.");
        }

        var missingDestinationIds = await store.FindMissingDestinationIdsAsync(
            destinations.Select(item => item.DestinationId).ToArray(),
            cancellationToken);
        if (missingDestinationIds.Count > 0)
        {
            throw new DestinationNotFoundException(
                $"Destination ids were not found: {string.Join(", ", missingDestinationIds)}.");
        }

        var links = destinations
            .Select(item => new ProductDestination
            {
                ProductId = id,
                DestinationId = item.DestinationId,
                VisitOrder = item.VisitOrder,
                IsMainDestination = item.IsMainDestination,
                Notes = NormalizeOptional(item.Notes)
            })
            .ToArray();
        await store.ReplaceProductDestinationsAsync(id, links, cancellationToken);
        return (await store.GetProductDestinationsAsync(id, cancellationToken))
            .Select(ToResponse)
            .ToArray();
    }

    public async Task RemoveDestinationAsync(
        int id,
        int destinationId,
        CancellationToken cancellationToken)
    {
        _ = await GetProductAsync(id, cancellationToken);
        if (destinationId <= 0)
        {
            throw new ArgumentException("Destination id must be positive.");
        }

        if (!await store.RemoveProductDestinationAsync(id, destinationId, cancellationToken))
        {
            throw new DestinationNotFoundException(
                $"Destination {destinationId} is not assigned to product {id}.");
        }
    }

    public async Task<ProductImageResponse> AddImageAsync(
        int id,
        string imageUrl,
        string? altText,
        bool isPrimary,
        int displayOrder,
        CancellationToken cancellationToken)
    {
        _ = await GetProductAsync(id, cancellationToken);
        if (displayOrder < 0)
        {
            throw new ArgumentException("DisplayOrder cannot be negative.");
        }

        var images = await store.GetProductImagesAsync(id, cancellationToken);
        var makePrimary = isPrimary || images.Count == 0;
        if (makePrimary)
        {
            foreach (var existingImage in images)
            {
                existingImage.IsPrimary = false;
            }
        }

        var image = new TourismProductImage
        {
            ProductId = id,
            ImageUrl = NormalizeRequired(imageUrl, nameof(imageUrl)),
            AltText = NormalizeOptional(altText),
            IsPrimary = makePrimary,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow
        };
        store.Add(image);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(image);
    }

    public async Task RemoveImageAsync(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        _ = await GetProductAsync(id, cancellationToken);
        if (imageId <= 0)
        {
            throw new ArgumentException("Image id must be positive.");
        }

        var image = await store.FindProductImageAsync(imageId, cancellationToken);
        if (image is null || image.ProductId != id)
        {
            throw new ProductNotFoundException($"Image {imageId} was not found for product {id}.");
        }

        var wasPrimary = image.IsPrimary;
        store.Remove(image);
        if (wasPrimary)
        {
            var replacement = (await store.GetProductImagesAsync(id, cancellationToken))
                .Where(candidate => candidate.Id != imageId)
                .OrderBy(candidate => candidate.DisplayOrder)
                .ThenBy(candidate => candidate.Id)
                .FirstOrDefault();
            if (replacement is not null)
            {
                replacement.IsPrimary = true;
            }
        }

        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductImageResponse> SetPrimaryImageAsync(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        _ = await GetProductAsync(id, cancellationToken);
        if (imageId <= 0)
        {
            throw new ArgumentException("Image id must be positive.");
        }

        var image = await store.FindProductImageAsync(imageId, cancellationToken);
        if (image is null || image.ProductId != id)
        {
            throw new ProductNotFoundException($"Image {imageId} was not found for product {id}.");
        }

        var images = await store.GetProductImagesAsync(id, cancellationToken);
        foreach (var productImage in images)
        {
            productImage.IsPrimary = productImage.Id == imageId;
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(image);
    }

    private async Task<TourismProduct> GetProductAsync(int id, CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindProductAsync(id, cancellationToken)
                ?? throw new ProductNotFoundException($"Product {id} was not found.")
            : throw new ArgumentException("Product id must be positive.");

    private async Task EnsureCodeAvailableAsync(
        string code,
        int? excludingProductId,
        CancellationToken cancellationToken)
    {
        if (await store.FindProductByCodeAsync(code, excludingProductId, cancellationToken) is not null)
        {
            throw new ProductConflictException($"Product code '{code}' is already in use.");
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

    private static void ValidateProductType(ProductType? productType)
    {
        if (productType.HasValue && !Enum.IsDefined(productType.Value))
        {
            throw new ArgumentException("ProductType must be 1 (Excursion) or 2 (Package).");
        }
    }

    private static void ValidateProduct(
        ProductType productType,
        int durationDays,
        int durationNights,
        decimal basePrice,
        string currency,
        int minimumPassengers,
        int? maximumPassengers)
    {
        ValidateProductType(productType);
        if (durationDays < 0 || durationNights < 0)
        {
            throw new ArgumentException("DurationDays and DurationNights cannot be negative.");
        }

        if (basePrice < 0)
        {
            throw new ArgumentException("BasePrice cannot be negative.");
        }

        _ = NormalizeCurrency(currency);
        if (minimumPassengers < 1 ||
            (maximumPassengers.HasValue && maximumPassengers.Value < minimumPassengers))
        {
            throw new ArgumentException(
                "MinimumPassengers must be positive and MaximumPassengers cannot be less than it.");
        }
    }

    private static string NormalizeCode(string? code) =>
        NormalizeRequired(code, nameof(code)).ToUpperInvariant();

    private static string NormalizeCurrency(string? currency)
    {
        var normalized = NormalizeRequired(currency, nameof(currency)).ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency must be a three-letter ISO currency code.");
        }

        return normalized;
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

    private static TourismProductResponse ToResponse(TourismProduct product) =>
        new(
            product.Id,
            product.Name,
            product.Code,
            product.Description,
            product.ProductType,
            product.DurationDays,
            product.DurationNights,
            product.BasePrice,
            product.Currency,
            product.MinimumPassengers,
            product.MaximumPassengers,
            product.Includes,
            product.Excludes,
            product.Recommendations,
            product.CancellationPolicy,
            product.IsPublished,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);

    private static ProductDestinationResponse ToResponse(ProductDestination link) =>
        new(
            link.ProductId,
            link.DestinationId,
            link.VisitOrder,
            link.IsMainDestination,
            link.Notes,
            link.Destination.Name,
            link.Destination.Country,
            link.Destination.City,
            link.Destination.DestinationType);

    private static ProductImageResponse ToResponse(TourismProductImage image) =>
        new(
            image.Id,
            image.ProductId,
            image.ImageUrl,
            image.AltText,
            image.IsPrimary,
            image.DisplayOrder,
            image.CreatedAt);

}
