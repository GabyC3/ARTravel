using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Itineraries;
using ARTravel.Application.DTOs.Itineraries;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.UseCases.Itineraries;

public sealed class ItineraryService(IItineraryStore store) : IItineraryService
{
    public async Task<PagedItineraries> ListAsync(
        int page,
        int pageSize,
        int? productId,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken)
    {
        var skip = GetSkip(page, pageSize);
        if (productId is <= 0)
        {
            throw new ArgumentException("ProductId must be positive.");
        }

        var result = await store.ListAsync(
            skip,
            pageSize,
            productId,
            isActive,
            NormalizeOptional(search),
            cancellationToken);
        return new PagedItineraries(
            result.Items.Select(ToResponse).ToArray(),
            page,
            pageSize,
            result.TotalCount);
    }

    public async Task<ItineraryDetailResponse> GetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var itinerary = await GetItineraryAsync(id, cancellationToken);
        var days = await store.GetDaysAsync(id, cancellationToken);
        return new ItineraryDetailResponse(
            ToResponse(itinerary),
            days.Select(ToResponse).ToArray());
    }

    public async Task<ItineraryDetailResponse> CreateAsync(
        CreateItineraryCommand command,
        CancellationToken cancellationToken)
    {
        await EnsureProductExistsAsync(command.ProductId, cancellationToken);
        var itinerary = new Itinerary
        {
            ProductId = command.ProductId,
            Name = NormalizeRequired(command.Name, nameof(command.Name)),
            Description = NormalizeOptional(command.Description),
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        store.Add(itinerary);
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(itinerary.Id, cancellationToken);
    }

    public async Task<ItineraryDetailResponse> UpdateAsync(
        int id,
        UpdateItineraryCommand command,
        CancellationToken cancellationToken)
    {
        var itinerary = await GetItineraryAsync(id, cancellationToken);
        await EnsureProductExistsAsync(command.ProductId, cancellationToken);
        itinerary.ProductId = command.ProductId;
        itinerary.Name = NormalizeRequired(command.Name, nameof(command.Name));
        itinerary.Description = NormalizeOptional(command.Description);
        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<ItineraryResponse> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var itinerary = await GetItineraryAsync(id, cancellationToken);
        if (itinerary.IsActive != isActive)
        {
            itinerary.IsActive = isActive;
            Touch(itinerary);
            await store.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(itinerary);
    }

    public async Task<ItineraryDetailResponse> DuplicateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var source = await GetItineraryAsync(id, cancellationToken);
        var sourceDays = await store.GetDaysAsync(id, cancellationToken);
        const string suffix = " (Copy)";
        var name = source.Name.Length + suffix.Length <= 200
            ? source.Name + suffix
            : source.Name[..(200 - suffix.Length)] + suffix;
        var duplicate = new Itinerary
        {
            ProductId = source.ProductId,
            Name = name,
            Description = source.Description,
            Version = checked(source.Version + 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var sourceDay in sourceDays)
        {
            var day = new ItineraryDay
            {
                Itinerary = duplicate,
                DayNumber = sourceDay.DayNumber,
                Title = sourceDay.Title,
                Description = sourceDay.Description,
                CreatedAt = DateTime.UtcNow
            };
            foreach (var sourceActivity in sourceDay.Activities)
            {
                day.Activities.Add(new ItineraryActivity
                {
                    ItineraryDay = day,
                    DestinationId = sourceActivity.DestinationId,
                    Name = sourceActivity.Name,
                    Description = sourceActivity.Description,
                    ActivityType = sourceActivity.ActivityType,
                    StartTime = sourceActivity.StartTime,
                    EndTime = sourceActivity.EndTime,
                    DurationMinutes = sourceActivity.DurationMinutes,
                    DisplayOrder = sourceActivity.DisplayOrder,
                    Notes = sourceActivity.Notes
                });
            }

            duplicate.Days.Add(day);
        }

        store.Add(duplicate);
        await store.SaveChangesAsync(cancellationToken);
        return await GetAsync(duplicate.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ItineraryDayResponse>> GetDaysAsync(
        int id,
        CancellationToken cancellationToken)
    {
        _ = await GetItineraryAsync(id, cancellationToken);
        var days = await store.GetDaysAsync(id, cancellationToken);
        return days.Select(ToResponse).ToArray();
    }

    public async Task<ItineraryDayResponse> AddDayAsync(
        int id,
        AddItineraryDayCommand command,
        CancellationToken cancellationToken)
    {
        var itinerary = await GetItineraryAsync(id, cancellationToken);
        var days = await store.GetDaysAsync(id, cancellationToken);
        var day = new ItineraryDay
        {
            Itinerary = itinerary,
            DayNumber = days.Count == 0 ? 1 : checked(days.Max(existing => existing.DayNumber) + 1),
            Title = NormalizeRequired(command.Title, nameof(command.Title)),
            Description = NormalizeOptional(command.Description),
            CreatedAt = DateTime.UtcNow
        };
        store.Add(day);
        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(day);
    }

    public async Task<ItineraryDayResponse> UpdateDayAsync(
        int id,
        int dayId,
        UpdateItineraryDayCommand command,
        CancellationToken cancellationToken)
    {
        var itinerary = await GetItineraryAsync(id, cancellationToken);
        var day = (await store.GetDaysAsync(id, cancellationToken))
            .SingleOrDefault(item => item.Id == dayId)
            ?? throw new ItineraryNotFoundException($"Day {dayId} was not found in itinerary {id}.");
        day.Title = NormalizeRequired(command.Title, nameof(command.Title));
        day.Description = NormalizeOptional(command.Description);
        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(day);
    }

    public async Task RemoveDayAsync(
        int id,
        int dayId,
        CancellationToken cancellationToken)
    {
        var itinerary = await GetItineraryAsync(id, cancellationToken);
        var days = (await store.GetDaysAsync(id, cancellationToken)).ToList();
        var day = days.SingleOrDefault(item => item.Id == dayId)
            ?? throw new ItineraryNotFoundException($"Day {dayId} was not found in itinerary {id}.");
        store.Remove(day);
        days.Remove(day);
        for (var index = 0; index < days.Count; index++)
        {
            days[index].DayNumber = index + 1;
        }

        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderDaysAsync(
        int id,
        IReadOnlyList<int> dayIds,
        CancellationToken cancellationToken)
    {
        var itinerary = await GetItineraryAsync(id, cancellationToken);
        var days = (await store.GetDaysAsync(id, cancellationToken)).ToDictionary(day => day.Id);
        ValidateOrder(dayIds, days.Keys, "DayIds");
        for (var index = 0; index < dayIds.Count; index++)
        {
            days[dayIds[index]].DayNumber = index + 1;
        }

        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ItineraryActivityResponse>> GetActivitiesAsync(
        int dayId,
        CancellationToken cancellationToken)
    {
        _ = await GetDayAsync(dayId, cancellationToken);
        var activities = await store.GetActivitiesAsync(dayId, cancellationToken);
        return activities.Select(ToResponse).ToArray();
    }

    public async Task<ItineraryActivityResponse> AddActivityAsync(
        int dayId,
        AddItineraryActivityCommand command,
        CancellationToken cancellationToken)
    {
        var day = await GetDayAsync(dayId, cancellationToken);
        var itinerary = await GetItineraryAsync(day.ItineraryId, cancellationToken);
        await EnsureDestinationExistsAsync(command.DestinationId, cancellationToken);
        ValidateActivity(
            command.ActivityType,
            command.StartTime,
            command.EndTime,
            command.DurationMinutes);

        var activities = (await store.GetActivitiesAsync(dayId, cancellationToken)).ToList();
        var displayOrder = command.DisplayOrder ?? activities.Count;
        if (displayOrder < 0 || displayOrder > activities.Count)
        {
            throw new ArgumentException("DisplayOrder must be between zero and the activity count.");
        }

        for (var index = activities.Count - 1; index >= displayOrder; index--)
        {
            activities[index].DisplayOrder++;
        }

        var activity = new ItineraryActivity
        {
            ItineraryDay = day,
            DestinationId = command.DestinationId,
            Name = NormalizeRequired(command.Name, nameof(command.Name)),
            Description = NormalizeOptional(command.Description),
            ActivityType = command.ActivityType,
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            DurationMinutes = command.DurationMinutes,
            DisplayOrder = displayOrder,
            Notes = NormalizeOptional(command.Notes)
        };
        store.Add(activity);
        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(activity);
    }

    public async Task<ItineraryActivityResponse> UpdateActivityAsync(
        int dayId,
        int activityId,
        UpdateItineraryActivityCommand command,
        CancellationToken cancellationToken)
    {
        var day = await GetDayAsync(dayId, cancellationToken);
        var itinerary = await GetItineraryAsync(day.ItineraryId, cancellationToken);
        ValidateActivity(
            command.ActivityType,
            command.StartTime,
            command.EndTime,
            command.DurationMinutes);
        var activity = (await store.GetActivitiesAsync(dayId, cancellationToken))
            .SingleOrDefault(item => item.Id == activityId)
            ?? throw new ItineraryNotFoundException(
                $"Activity {activityId} was not found in itinerary day {dayId}.");
        await EnsureDestinationExistsAsync(command.DestinationId, cancellationToken);
        activity.DestinationId = command.DestinationId;
        activity.Name = NormalizeRequired(command.Name, nameof(command.Name));
        activity.Description = NormalizeOptional(command.Description);
        activity.ActivityType = command.ActivityType;
        activity.StartTime = command.StartTime;
        activity.EndTime = command.EndTime;
        activity.DurationMinutes = command.DurationMinutes;
        activity.Notes = NormalizeOptional(command.Notes);
        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
        return ToResponse(activity);
    }

    public async Task RemoveActivityAsync(
        int dayId,
        int activityId,
        CancellationToken cancellationToken)
    {
        var day = await GetDayAsync(dayId, cancellationToken);
        var itinerary = await GetItineraryAsync(day.ItineraryId, cancellationToken);
        var activities = (await store.GetActivitiesAsync(dayId, cancellationToken)).ToList();
        var activity = activities.SingleOrDefault(item => item.Id == activityId)
            ?? throw new ItineraryNotFoundException(
                $"Activity {activityId} was not found in itinerary day {dayId}.");
        store.Remove(activity);
        activities.Remove(activity);
        for (var index = 0; index < activities.Count; index++)
        {
            activities[index].DisplayOrder = index;
        }

        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderActivitiesAsync(
        int dayId,
        IReadOnlyList<int> activityIds,
        CancellationToken cancellationToken)
    {
        var day = await GetDayAsync(dayId, cancellationToken);
        var itinerary = await GetItineraryAsync(day.ItineraryId, cancellationToken);
        var activities = (await store.GetActivitiesAsync(dayId, cancellationToken))
            .ToDictionary(activity => activity.Id);
        ValidateOrder(activityIds, activities.Keys, "ActivityIds");
        for (var index = 0; index < activityIds.Count; index++)
        {
            activities[activityIds[index]].DisplayOrder = index;
        }

        Touch(itinerary);
        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task<Itinerary> GetItineraryAsync(int id, CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindItineraryAsync(id, cancellationToken)
                ?? throw new ItineraryNotFoundException($"Itinerary {id} was not found.")
            : throw new ArgumentException("Itinerary id must be positive.");

    private async Task<ItineraryDay> GetDayAsync(int id, CancellationToken cancellationToken) =>
        id > 0
            ? await store.FindDayAsync(id, cancellationToken)
                ?? throw new ItineraryNotFoundException($"Itinerary day {id} was not found.")
            : throw new ArgumentException("Itinerary day id must be positive.");

    private async Task EnsureProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new ArgumentException("ProductId must be positive.");
        }

        if (!await store.ProductExistsAsync(productId, cancellationToken))
        {
            throw new ProductNotFoundException($"Product {productId} was not found.");
        }
    }

    private async Task EnsureDestinationExistsAsync(
        int destinationId,
        CancellationToken cancellationToken)
    {
        if (destinationId <= 0)
        {
            throw new ArgumentException("DestinationId must be positive.");
        }

        if (!await store.DestinationExistsAsync(destinationId, cancellationToken))
        {
            throw new DestinationNotFoundException($"Destination {destinationId} was not found.");
        }
    }

    private static void Touch(Itinerary itinerary)
    {
        itinerary.Version = checked(itinerary.Version + 1);
        itinerary.UpdatedAt = DateTime.UtcNow;
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

    private static void ValidateOrder(
        IReadOnlyList<int> requestedIds,
        IEnumerable<int> existingIds,
        string fieldName)
    {
        var existing = existingIds.ToHashSet();
        if (requestedIds.Count != existing.Count ||
            requestedIds.Distinct().Count() != requestedIds.Count ||
            !requestedIds.ToHashSet().SetEquals(existing))
        {
            throw new ArgumentException($"{fieldName} must contain every existing id exactly once.");
        }
    }

    private static void ValidateActivity(
        ItineraryActivityType activityType,
        TimeOnly? startTime,
        TimeOnly? endTime,
        int durationMinutes)
    {
        if (!Enum.IsDefined(activityType))
        {
            throw new ArgumentException("ActivityType must be between 1 and 8.");
        }

        if (durationMinutes < 0)
        {
            throw new ArgumentException("DurationMinutes cannot be negative.");
        }

        if (startTime.HasValue != endTime.HasValue ||
            (startTime.HasValue && endTime <= startTime))
        {
            throw new ArgumentException(
                "StartTime and EndTime must be provided together, and EndTime must be later.");
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

    private static ItineraryResponse ToResponse(Itinerary itinerary) =>
        new(
            itinerary.Id,
            itinerary.ProductId,
            itinerary.Name,
            itinerary.Description,
            itinerary.Version,
            itinerary.IsActive,
            itinerary.CreatedAt,
            itinerary.UpdatedAt);

    private static ItineraryDayResponse ToResponse(ItineraryDay day) =>
        new(
            day.Id,
            day.ItineraryId,
            day.DayNumber,
            day.Title,
            day.Description,
            day.CreatedAt,
            day.Activities
                .OrderBy(activity => activity.DisplayOrder)
                .ThenBy(activity => activity.Id)
                .Select(ToResponse)
                .ToArray());

    private static ItineraryActivityResponse ToResponse(ItineraryActivity activity) =>
        new(
            activity.Id,
            activity.ItineraryDayId,
            activity.DestinationId,
            activity.Name,
            activity.Description,
            activity.ActivityType,
            activity.StartTime,
            activity.EndTime,
            activity.DurationMinutes,
            activity.DisplayOrder,
            activity.Notes);
}
