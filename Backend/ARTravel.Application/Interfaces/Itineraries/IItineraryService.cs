using ARTravel.Application.DTOs.Itineraries;

namespace ARTravel.Application.Interfaces.Itineraries;

public interface IItineraryService
{
    Task<PagedItineraries> ListAsync(
        int page,
        int pageSize,
        int? productId,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken);

    Task<ItineraryDetailResponse> GetAsync(int id, CancellationToken cancellationToken);

    Task<ItineraryDetailResponse> CreateAsync(
        CreateItineraryCommand command,
        CancellationToken cancellationToken);

    Task<ItineraryDetailResponse> UpdateAsync(
        int id,
        UpdateItineraryCommand command,
        CancellationToken cancellationToken);

    Task<ItineraryResponse> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken);

    Task<ItineraryDetailResponse> DuplicateAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItineraryDayResponse>> GetDaysAsync(
        int id,
        CancellationToken cancellationToken);

    Task<ItineraryDayResponse> AddDayAsync(
        int id,
        AddItineraryDayCommand command,
        CancellationToken cancellationToken);

    Task<ItineraryDayResponse> UpdateDayAsync(
        int id,
        int dayId,
        UpdateItineraryDayCommand command,
        CancellationToken cancellationToken);

    Task RemoveDayAsync(int id, int dayId, CancellationToken cancellationToken);

    Task ReorderDaysAsync(
        int id,
        IReadOnlyList<int> dayIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ItineraryActivityResponse>> GetActivitiesAsync(
        int dayId,
        CancellationToken cancellationToken);

    Task<ItineraryActivityResponse> AddActivityAsync(
        int dayId,
        AddItineraryActivityCommand command,
        CancellationToken cancellationToken);

    Task<ItineraryActivityResponse> UpdateActivityAsync(
        int dayId,
        int activityId,
        UpdateItineraryActivityCommand command,
        CancellationToken cancellationToken);

    Task RemoveActivityAsync(int dayId, int activityId, CancellationToken cancellationToken);

    Task ReorderActivitiesAsync(
        int dayId,
        IReadOnlyList<int> activityIds,
        CancellationToken cancellationToken);
}
