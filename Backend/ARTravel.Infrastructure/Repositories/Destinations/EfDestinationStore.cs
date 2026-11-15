using ARTravel.Application.Interfaces.Destinations;
using ARTravel.Domain.Entities;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ARTravel.Infrastructure.Repositories.Destinations;

public sealed class EfDestinationStore(AppDbContext dbContext) : IDestinationStore
{
    public async Task<(IReadOnlyList<Destination> Items, int TotalCount)> ListDestinationsAsync(
        int skip,
        int take,
        string? search,
        DestinationType? destinationType,
        string? country,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Destinations.AsNoTracking();
        if (destinationType.HasValue)
        {
            query = query.Where(destination => destination.DestinationType == destinationType.Value);
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(destination => EF.Functions.ILike(destination.Country, country.Trim()));
        }

        if (isActive.HasValue)
        {
            query = query.Where(destination => destination.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(destination =>
                EF.Functions.ILike(destination.Name, pattern) ||
                EF.Functions.ILike(destination.Description ?? string.Empty, pattern) ||
                EF.Functions.ILike(destination.Country, pattern) ||
                EF.Functions.ILike(destination.Province ?? string.Empty, pattern) ||
                EF.Functions.ILike(destination.City ?? string.Empty, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(destination => destination.Name)
            .ThenBy(destination => destination.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<Destination?> FindDestinationAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Destinations.SingleOrDefaultAsync(
            destination => destination.Id == id,
            cancellationToken);

    public async Task<(IReadOnlyList<Destination> Items, int TotalCount)> ListChildDestinationsAsync(
        int parentId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Destinations
            .AsNoTracking()
            .Where(destination => destination.ParentDestinationId == parentId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(destination => destination.Name)
            .ThenBy(destination => destination.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<TourismProduct> Items, int TotalCount)> ListDestinationProductsAsync(
        int destinationId,
        int skip,
        int take,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ProductDestinations
            .AsNoTracking()
            .Where(link => link.DestinationId == destinationId)
            .Select(link => link.TourismProduct);
        if (isActive.HasValue)
        {
            query = query.Where(product => product.IsActive == isActive.Value);
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

    public Task<int> CountChildDestinationsAsync(
        int destinationId,
        CancellationToken cancellationToken) =>
        dbContext.Destinations.CountAsync(
            destination => destination.ParentDestinationId == destinationId,
            cancellationToken);

    public Task<int> CountDestinationProductsAsync(
        int destinationId,
        CancellationToken cancellationToken) =>
        dbContext.ProductDestinations.CountAsync(
            link => link.DestinationId == destinationId,
            cancellationToken);

    public Task<int> CountDestinationImagesAsync(
        int destinationId,
        CancellationToken cancellationToken) =>
        dbContext.DestinationImages.CountAsync(
            image => image.DestinationId == destinationId,
            cancellationToken);

    public async Task<IReadOnlyList<DestinationImage>> GetDestinationImagesAsync(
        int destinationId,
        CancellationToken cancellationToken) =>
        await dbContext.DestinationImages
            .Where(image => image.DestinationId == destinationId)
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .ToListAsync(cancellationToken);

    public Task<DestinationImage?> FindDestinationImageAsync(
        int imageId,
        CancellationToken cancellationToken) =>
        dbContext.DestinationImages.SingleOrDefaultAsync(
            image => image.Id == imageId,
            cancellationToken);

    public void Add(Destination destination) => dbContext.Destinations.Add(destination);

    public void Add(DestinationImage image) => dbContext.DestinationImages.Add(image);

    public void Remove(DestinationImage image) => dbContext.DestinationImages.Remove(image);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
