using ARTravel.Application.Interfaces.Departures;
using ARTravel.Application.DTOs.Departures;
using ARTravel.Domain.Exceptions;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.UseCases.Departures;

public sealed class DepartureService(IDepartureStore store) : IDepartureService
{
    public async Task<PagedDepartures> ListAsync(
        int page,
        int pageSize,
        int? productId,
        DepartureStatus? status,
        CancellationToken cancellationToken)
    {
        var skip = GetSkip(page, pageSize);
        ValidateStatus(status);
        if (productId is <= 0)
        {
            throw new ArgumentException("ProductId must be positive.");
        }

        var result = await store.ListAsync(
            productId,
            status,
            skip,
            pageSize,
            cancellationToken);
        return new PagedDepartures(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<DepartureResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        ToResponse(await GetDepartureAsync(id, cancellationToken));

    public async Task<DepartureResponse> CreateAsync(
        CreateDepartureCommand command,
        CancellationToken cancellationToken)
    {
        ValidateDeparture(
            command.DepartureDate,
            command.ReturnDate,
            command.Capacity,
            command.MinimumPassengers,
            command.BasePrice,
            command.Currency,
            command.Status);
        var product = await GetProductAsync(command.ProductId, cancellationToken);
        await EnsureItineraryBelongsToProductAsync(
            command.ItineraryId,
            product.Id,
            cancellationToken);

        var departure = new Departure
        {
            ProductId = product.Id,
            ItineraryId = command.ItineraryId,
            DepartureDate = command.DepartureDate,
            ReturnDate = command.ReturnDate,
            MeetingPoint = NormalizeRequired(command.MeetingPoint, nameof(command.MeetingPoint)),
            MeetingTime = command.MeetingTime,
            Capacity = command.Capacity,
            MinimumPassengers = command.MinimumPassengers,
            BasePrice = command.BasePrice,
            Currency = NormalizeCurrency(command.Currency),
            Status = command.Status,
            Notes = NormalizeOptional(command.Notes),
            CreatedAt = DateTime.UtcNow
        };
        store.Add(departure);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(departure);
    }

    public Task<DepartureResponse> CreateForProductAsync(
        int productId,
        CreateDepartureCommand command,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentException("Product id must be positive.");
        }

        return CreateAsync(command with { ProductId = productId }, cancellationToken);
    }

    public async Task<DepartureResponse> UpdateAsync(
        int id,
        UpdateDepartureCommand command,
        CancellationToken cancellationToken)
    {
        var departure = await GetDepartureAsync(id, cancellationToken);
        ValidateDeparture(
            command.DepartureDate,
            command.ReturnDate,
            command.Capacity,
            command.MinimumPassengers,
            command.BasePrice,
            command.Currency,
            departure.Status);
        await EnsureItineraryBelongsToProductAsync(
            command.ItineraryId,
            departure.ProductId,
            cancellationToken);

        departure.ItineraryId = command.ItineraryId;
        departure.DepartureDate = command.DepartureDate;
        departure.ReturnDate = command.ReturnDate;
        departure.MeetingPoint = NormalizeRequired(command.MeetingPoint, nameof(command.MeetingPoint));
        departure.MeetingTime = command.MeetingTime;
        departure.Capacity = command.Capacity;
        departure.MinimumPassengers = command.MinimumPassengers;
        departure.BasePrice = command.BasePrice;
        departure.Currency = NormalizeCurrency(command.Currency);
        departure.Notes = NormalizeOptional(command.Notes);
        departure.UpdatedAt = DateTime.UtcNow;
        if (!await store.UpdateIfCapacityAllowsAsync(departure, cancellationToken))
        {
            var passengerCount = await store.CountPassengersAsync(id, cancellationToken);
            throw new DepartureConflictException(
                $"Capacity cannot be lower than the {passengerCount} passengers assigned to this departure.");
        }

        return ToResponse(departure);
    }

    public async Task<DepartureResponse> SetStatusAsync(
        int id,
        DepartureStatus status,
        CancellationToken cancellationToken)
    {
        ValidateStatus(status);
        var departure = await GetDepartureAsync(id, cancellationToken);
        departure.Status = status;
        departure.UpdatedAt = DateTime.UtcNow;
        if (!await store.UpdateIfCapacityAllowsAsync(departure, cancellationToken))
        {
            throw new DepartureConflictException(
                "The departure capacity is lower than its assigned passenger count.");
        }

        return ToResponse(departure);
    }

    public Task<DepartureResponse> CancelAsync(int id, CancellationToken cancellationToken) =>
        SetStatusAsync(id, DepartureStatus.Cancelled, cancellationToken);

    public async Task<PagedDepartures> GetProductDeparturesAsync(
        int productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        _ = await GetProductAsync(productId, cancellationToken);
        return await ListAsync(page, pageSize, productId, null, cancellationToken);
    }

    public async Task<DepartureAvailability> GetAvailabilityAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var departure = await GetDepartureAsync(id, cancellationToken);
        var passengerCount = await store.CountPassengersAsync(id, cancellationToken);
        return new DepartureAvailability(
            departure.Id,
            departure.Capacity,
            passengerCount,
            Math.Max(0, departure.Capacity - passengerCount),
            departure.MinimumPassengers,
            passengerCount >= departure.Capacity,
            departure.Status);
    }

    public async Task<IReadOnlyList<DeparturePriceResponse>> GetPricesAsync(
        int id,
        CancellationToken cancellationToken)
    {
        _ = await GetDepartureAsync(id, cancellationToken);
        return (await store.ListPricesAsync(id, cancellationToken))
            .Select(ToResponse)
            .ToArray();
    }

    public async Task<DeparturePriceResponse> AddPriceAsync(
        int id,
        CreateDeparturePriceCommand command,
        CancellationToken cancellationToken)
    {
        _ = await GetDepartureAsync(id, cancellationToken);
        ValidatePrice(command);
        var price = new DeparturePrice
        {
            DepartureId = id,
            PriceType = command.PriceType,
            Amount = command.Amount,
            Currency = NormalizeCurrency(command.Currency),
            Description = NormalizeOptional(command.Description)
        };
        store.Add(price);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(price);
    }

    public async Task<DeparturePriceResponse> UpdatePriceAsync(
        int id,
        int priceId,
        CreateDeparturePriceCommand command,
        CancellationToken cancellationToken)
    {
        _ = await GetDepartureAsync(id, cancellationToken);
        if (priceId <= 0)
        {
            throw new ArgumentException("Price id must be positive.");
        }

        ValidatePrice(command);
        var price = await store.FindPriceAsync(priceId, cancellationToken);
        if (price is null || price.DepartureId != id)
        {
            throw new DepartureNotFoundException(
                $"Price {priceId} was not found for departure {id}.");
        }

        price.PriceType = command.PriceType;
        price.Amount = command.Amount;
        price.Currency = NormalizeCurrency(command.Currency);
        price.Description = NormalizeOptional(command.Description);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(price);
    }

    public async Task RemovePriceAsync(
        int id,
        int priceId,
        CancellationToken cancellationToken)
    {
        _ = await GetDepartureAsync(id, cancellationToken);
        if (priceId <= 0)
        {
            throw new ArgumentException("Price id must be positive.");
        }

        var price = await store.FindPriceAsync(priceId, cancellationToken);
        if (price is null || price.DepartureId != id)
        {
            throw new DepartureNotFoundException(
                $"Price {priceId} was not found for departure {id}.");
        }

        store.Remove(price);
        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task<Departure> GetDepartureAsync(int id, CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindAsync(id, cancellationToken)
                ?? throw new DepartureNotFoundException($"Departure {id} was not found.")
            : throw new ArgumentException("Departure id must be positive.");

    private async Task<TourismProduct> GetProductAsync(int productId, CancellationToken cancellationToken) =>
        productId > 0
            ? await store.FindProductAsync(productId, cancellationToken)
                ?? throw new ProductNotFoundException($"Product {productId} was not found.")
            : throw new ArgumentException("Product id must be positive.");

    private async Task EnsureItineraryBelongsToProductAsync(
        int itineraryId,
        int productId,
        CancellationToken cancellationToken)
    {
        if (itineraryId <= 0)
        {
            throw new ArgumentException("ItineraryId must be positive.");
        }

        var itinerary = await store.FindItineraryAsync(itineraryId, cancellationToken);
        if (itinerary is null || itinerary.ProductId != productId)
        {
            throw new ItineraryNotFoundException(
                $"Itinerary {itineraryId} was not found for product {productId}.");
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

    private static void ValidateDeparture(
        DateTime departureDate,
        DateTime returnDate,
        int capacity,
        int minimumPassengers,
        decimal basePrice,
        string currency,
        DepartureStatus status)
    {
        if (departureDate == default || departureDate.Kind != DateTimeKind.Utc ||
            returnDate == default || returnDate.Kind != DateTimeKind.Utc ||
            returnDate <= departureDate)
        {
            throw new ArgumentException(
                "DepartureDate and ReturnDate must be UTC dates, and ReturnDate must be later.");
        }

        if (capacity < 1 || minimumPassengers < 1 || minimumPassengers > capacity)
        {
            throw new ArgumentException(
                "Capacity must be positive and MinimumPassengers must be between 1 and Capacity.");
        }

        if (basePrice < 0)
        {
            throw new ArgumentException("BasePrice cannot be negative.");
        }

        _ = NormalizeCurrency(currency);
        ValidateStatus(status);
    }

    private static void ValidatePrice(CreateDeparturePriceCommand command)
    {
        if (!Enum.IsDefined(command.PriceType))
        {
            throw new ArgumentException("PriceType must be between 1 (Standard) and 4 (Promotional).");
        }

        if (command.Amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative.");
        }

        _ = NormalizeCurrency(command.Currency);
    }

    private static void ValidateStatus(DepartureStatus? status)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentException("Status must be between 1 (Scheduled) and 7 (Suspended).");
        }
    }

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

    private static DepartureResponse ToResponse(Departure departure) =>
        new(
            departure.Id,
            departure.ProductId,
            departure.ItineraryId,
            departure.DepartureDate,
            departure.ReturnDate,
            departure.MeetingPoint,
            departure.MeetingTime,
            departure.Capacity,
            departure.MinimumPassengers,
            departure.BasePrice,
            departure.Currency,
            departure.Status,
            departure.Notes,
            departure.CreatedAt,
            departure.UpdatedAt);

    private static DeparturePriceResponse ToResponse(DeparturePrice price) =>
        new(
            price.Id,
            price.DepartureId,
            price.PriceType,
            price.Amount,
            price.Currency,
            price.Description);
}
