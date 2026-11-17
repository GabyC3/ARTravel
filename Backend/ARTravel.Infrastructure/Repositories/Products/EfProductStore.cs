using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Products;
using ARTravel.Domain.Entities;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ARTravel.Infrastructure.Repositories.Products;

public sealed class EfProductStore(AppDbContext dbContext) : IProductStore
{
    public async Task<(IReadOnlyList<TourismProduct> Items, int TotalCount)> ListProductsAsync(
        int skip,
        int take,
        string? search,
        ProductType? productType,
        bool? isPublished,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();
        if (productType.HasValue)
        {
            query = query.Where(product => product.ProductType == productType.Value);
        }

        if (isPublished.HasValue)
        {
            query = query.Where(product => product.IsPublished == isPublished.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(product => product.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Code, pattern) ||
                EF.Functions.ILike(product.Description ?? string.Empty, pattern) ||
                dbContext.ProductDestinations.Any(link =>
                    link.ProductId == product.Id &&
                    (EF.Functions.ILike(link.Destination.Name, pattern) ||
                     EF.Functions.ILike(link.Destination.Country, pattern) ||
                     EF.Functions.ILike(link.Destination.Province ?? string.Empty, pattern) ||
                     EF.Functions.ILike(link.Destination.City ?? string.Empty, pattern))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<TourismProduct?> FindProductAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task<TourismProduct?> FindProductByCodeAsync(
        string code,
        int? excludingProductId,
        CancellationToken cancellationToken) =>
        dbContext.Products.SingleOrDefaultAsync(
            product => product.Code == code &&
                       (!excludingProductId.HasValue || product.Id != excludingProductId.Value),
            cancellationToken);

    public async Task<IReadOnlyDictionary<int, string>> GetPrimaryImageUrlsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var images = await dbContext.TourismProductImages
            .AsNoTracking()
            .Where(image => productIds.Contains(image.ProductId))
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Select(image => new { image.ProductId, image.ImageUrl })
            .ToListAsync(cancellationToken);

        return images
            .GroupBy(image => image.ProductId)
            .ToDictionary(group => group.Key, group => group.First().ImageUrl);
    }

    public async Task<bool> HasReferencesAsync(int productId, CancellationToken cancellationToken) =>
        await dbContext.Itineraries.AnyAsync(
            itinerary => itinerary.ProductId == productId,
            cancellationToken) ||
        await dbContext.Departures.AnyAsync(
            departure => departure.ProductId == productId,
            cancellationToken);

    public async Task<IReadOnlyList<ProductDestination>> GetProductDestinationsAsync(
        int productId,
        CancellationToken cancellationToken) =>
        await dbContext.ProductDestinations
            .Include(link => link.Destination)
            .Where(link => link.ProductId == productId)
            .OrderBy(link => link.VisitOrder)
            .ThenBy(link => link.DestinationId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<int>> FindMissingDestinationIdsAsync(
        IReadOnlyCollection<int> destinationIds,
        CancellationToken cancellationToken)
    {
        if (destinationIds.Count == 0)
        {
            return [];
        }

        var existingIds = await dbContext.Destinations
            .Where(destination => destinationIds.Contains(destination.Id))
            .Select(destination => destination.Id)
            .ToListAsync(cancellationToken);
        return destinationIds.Except(existingIds).ToArray();
    }

    public async Task ReplaceProductDestinationsAsync(
        int productId,
        IReadOnlyList<ProductDestination> destinations,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await dbContext.ProductDestinations
            .Where(link => link.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
        dbContext.ProductDestinations.AddRange(destinations);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> RemoveProductDestinationAsync(
        int productId,
        int destinationId,
        CancellationToken cancellationToken) =>
        await dbContext.ProductDestinations
            .Where(link => link.ProductId == productId && link.DestinationId == destinationId)
            .ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<IReadOnlyList<TourismProductImage>> GetProductImagesAsync(
        int productId,
        CancellationToken cancellationToken) =>
        await dbContext.TourismProductImages
            .Where(image => image.ProductId == productId)
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .ToListAsync(cancellationToken);

    public Task<TourismProductImage?> FindProductImageAsync(
        int imageId,
        CancellationToken cancellationToken) =>
        dbContext.TourismProductImages.SingleOrDefaultAsync(
            image => image.Id == imageId,
            cancellationToken);

    public void Add(TourismProduct product) => dbContext.Products.Add(product);

    public void Remove(TourismProduct product) => dbContext.Products.Remove(product);

    public void Add(TourismProductImage image) => dbContext.TourismProductImages.Add(image);

    public void Remove(TourismProductImage image) => dbContext.TourismProductImages.Remove(image);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Products_Code"
            })
        {
            throw new ARTravel.Domain.Exceptions.ProductConflictException(
                "A product with this code already exists.");
        }
    }
}
