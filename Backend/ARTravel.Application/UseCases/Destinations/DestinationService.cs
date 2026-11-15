using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Destinations;
using ARTravel.Application.DTOs.Destinations;
using ARTravel.Application.DTOs.Products;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.UseCases.Destinations;

public sealed class DestinationService(IDestinationStore store) : IDestinationService
{
    public async Task<PagedDestinations> ListAsync(
        int page,
        int pageSize,
        string? search,
        DestinationType? destinationType,
        string? country,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var skip = GetSkip(page, pageSize);
        ValidateDestinationType(destinationType);
        var result = await store.ListDestinationsAsync(
            skip,
            pageSize,
            NormalizeOptional(search),
            destinationType,
            NormalizeOptional(country),
            isActive,
            cancellationToken);
        return new PagedDestinations(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<DestinationDetail> GetAsync(int id, CancellationToken cancellationToken)
    {
        var destination = await GetDestinationAsync(id, cancellationToken);
        var images = await store.GetDestinationImagesAsync(id, cancellationToken);
        return new DestinationDetail(ToResponse(destination), images.Select(ToResponse).ToArray());
    }

    public async Task<DestinationDetail> CreateAsync(
        CreateDestinationCommand command,
        CancellationToken cancellationToken)
    {
        ValidateDestinationType(command.DestinationType);
        ValidateLocation(command.Latitude, command.Longitude, command.AltitudeMeters);
        await ValidateParentAsync(command.ParentDestinationId, null, cancellationToken);

        var destination = new Destination
        {
            Name = NormalizeRequired(command.Name, nameof(command.Name)),
            Description = NormalizeOptional(command.Description),
            DestinationType = command.DestinationType,
            Country = NormalizeRequired(command.Country, nameof(command.Country)),
            Province = NormalizeOptional(command.Province),
            City = NormalizeOptional(command.City),
            Address = NormalizeOptional(command.Address),
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            AltitudeMeters = command.AltitudeMeters,
            ParentDestinationId = command.ParentDestinationId,
            IsActive = command.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        store.Add(destination);
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(destination.Id, cancellationToken);
    }

    public async Task<DestinationDetail> UpdateAsync(
        int id,
        UpdateDestinationCommand command,
        CancellationToken cancellationToken)
    {
        ValidateDestinationType(command.DestinationType);
        ValidateLocation(command.Latitude, command.Longitude, command.AltitudeMeters);
        var destination = await GetDestinationAsync(id, cancellationToken);
        await ValidateParentAsync(command.ParentDestinationId, id, cancellationToken);

        destination.Name = NormalizeRequired(command.Name, nameof(command.Name));
        destination.Description = NormalizeOptional(command.Description);
        destination.DestinationType = command.DestinationType;
        destination.Country = NormalizeRequired(command.Country, nameof(command.Country));
        destination.Province = NormalizeOptional(command.Province);
        destination.City = NormalizeOptional(command.City);
        destination.Address = NormalizeOptional(command.Address);
        destination.Latitude = command.Latitude;
        destination.Longitude = command.Longitude;
        destination.AltitudeMeters = command.AltitudeMeters;
        destination.ParentDestinationId = command.ParentDestinationId;
        destination.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<DestinationResponse> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var destination = await GetDestinationAsync(id, cancellationToken);
        destination.IsActive = isActive;
        destination.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(destination);
    }

    public async Task<PagedDestinations> GetChildrenAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _ = await GetDestinationAsync(id, cancellationToken);
        var skip = GetSkip(page, pageSize);
        var result = await store.ListChildDestinationsAsync(
            id,
            skip,
            pageSize,
            cancellationToken);
        return new PagedDestinations(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<PagedProducts> GetProductsAsync(
        int id,
        int page,
        int pageSize,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        _ = await GetDestinationAsync(id, cancellationToken);
        var skip = GetSkip(page, pageSize);
        var result = await store.ListDestinationProductsAsync(
            id,
            skip,
            pageSize,
            isActive,
            cancellationToken);
        return new PagedProducts(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<DestinationSummary> GetSummaryAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var destination = await GetDestinationAsync(id, cancellationToken);
        var childCount = await store.CountChildDestinationsAsync(id, cancellationToken);
        var productCount = await store.CountDestinationProductsAsync(id, cancellationToken);
        var imageCount = await store.CountDestinationImagesAsync(id, cancellationToken);
        return new DestinationSummary(ToResponse(destination), childCount, productCount, imageCount);
    }

    public async Task<DestinationImageResponse> AddImageAsync(
        int id,
        AddDestinationImageCommand command,
        CancellationToken cancellationToken)
    {
        _ = await GetDestinationAsync(id, cancellationToken);
        if (command.DisplayOrder < 0)
        {
            throw new ArgumentException("DisplayOrder cannot be negative.");
        }

        var images = await store.GetDestinationImagesAsync(id, cancellationToken);
        var makePrimary = command.IsPrimary || images.Count == 0;
        if (makePrimary)
        {
            foreach (var existingImage in images)
            {
                existingImage.IsPrimary = false;
            }
        }

        var image = new DestinationImage
        {
            DestinationId = id,
            ImageUrl = NormalizeRequired(command.ImageUrl, nameof(command.ImageUrl)),
            AltText = NormalizeOptional(command.AltText),
            IsPrimary = makePrimary,
            DisplayOrder = command.DisplayOrder,
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
        _ = await GetDestinationAsync(id, cancellationToken);
        if (imageId <= 0)
        {
            throw new ArgumentException("Image id must be positive.");
        }

        var image = await store.FindDestinationImageAsync(imageId, cancellationToken);
        if (image is null || image.DestinationId != id)
        {
            throw new DestinationNotFoundException(
                $"Image {imageId} was not found for destination {id}.");
        }

        var wasPrimary = image.IsPrimary;
        store.Remove(image);
        if (wasPrimary)
        {
            var replacement = (await store.GetDestinationImagesAsync(id, cancellationToken))
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

    public async Task<DestinationImageResponse> SetPrimaryImageAsync(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        _ = await GetDestinationAsync(id, cancellationToken);
        if (imageId <= 0)
        {
            throw new ArgumentException("Image id must be positive.");
        }

        var image = await store.FindDestinationImageAsync(imageId, cancellationToken);
        if (image is null || image.DestinationId != id)
        {
            throw new DestinationNotFoundException(
                $"Image {imageId} was not found for destination {id}.");
        }

        var images = await store.GetDestinationImagesAsync(id, cancellationToken);
        foreach (var destinationImage in images)
        {
            destinationImage.IsPrimary = destinationImage.Id == imageId;
        }

        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(image);
    }

    private async Task<Destination> GetDestinationAsync(int id, CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindDestinationAsync(id, cancellationToken)
                ?? throw new DestinationNotFoundException($"Destination {id} was not found.")
            : throw new ArgumentException("Destination id must be positive.");

    private async Task ValidateParentAsync(
        int? parentId,
        int? destinationId,
        CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return;
        }

        var visited = new HashSet<int>();
        var currentId = parentId;
        while (currentId.HasValue)
        {
            if (currentId == destinationId)
            {
                throw new ArgumentException("A destination cannot be its own parent or descendant.");
            }

            if (!visited.Add(currentId.Value))
            {
                throw new InvalidOperationException("The destination hierarchy contains a cycle.");
            }

            var parent = await store.FindDestinationAsync(currentId.Value, cancellationToken)
                ?? throw new DestinationNotFoundException(
                    $"Parent destination {currentId.Value} was not found.");
            currentId = parent.ParentDestinationId;
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

    private static void ValidateDestinationType(DestinationType? destinationType)
    {
        if (destinationType.HasValue && !Enum.IsDefined(destinationType.Value))
        {
            throw new ArgumentException("DestinationType must be between 1 and 7.");
        }
    }

    private static void ValidateLocation(decimal? latitude, decimal? longitude, int? altitudeMeters)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException("Latitude and Longitude must either both be provided or both be omitted.");
        }

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentException("Longitude must be between -180 and 180.");
        }

        if (altitudeMeters < 0)
        {
            throw new ArgumentException("AltitudeMeters cannot be negative.");
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

    private static DestinationResponse ToResponse(Destination destination) =>
        new(
            destination.Id,
            destination.Name,
            destination.Description,
            destination.DestinationType,
            destination.Country,
            destination.Province,
            destination.City,
            destination.Address,
            destination.Latitude,
            destination.Longitude,
            destination.AltitudeMeters,
            destination.ParentDestinationId,
            destination.IsActive,
            destination.CreatedAt,
            destination.UpdatedAt);

    private static DestinationImageResponse ToResponse(DestinationImage image) =>
        new(
            image.Id,
            image.DestinationId,
            image.ImageUrl,
            image.AltText,
            image.IsPrimary,
            image.DisplayOrder,
            image.CreatedAt);

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
}
