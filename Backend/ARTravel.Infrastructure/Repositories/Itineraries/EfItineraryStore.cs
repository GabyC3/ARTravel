using ARTravel.Application.Interfaces.Itineraries;
using ARTravel.Domain.Entities;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ARTravel.Domain.Exceptions;

namespace ARTravel.Infrastructure.Repositories.Itineraries;

public sealed class EfItineraryStore(AppDbContext dbContext) : IItineraryStore
{
    public async Task<(IReadOnlyList<Itinerary> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        int? productId,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Itineraries.AsNoTracking();
        if (productId.HasValue)
        {
            query = query.Where(itinerary => itinerary.ProductId == productId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(itinerary => itinerary.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(itinerary =>
                EF.Functions.ILike(itinerary.Name, pattern) ||
                EF.Functions.ILike(itinerary.Description ?? string.Empty, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(itinerary => itinerary.ProductId)
            .ThenBy(itinerary => itinerary.Version)
            .ThenBy(itinerary => itinerary.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<Itinerary?> FindItineraryAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Itineraries.SingleOrDefaultAsync(
            itinerary => itinerary.Id == id,
            cancellationToken);

    public Task<ItineraryDay?> FindDayAsync(int dayId, CancellationToken cancellationToken) =>
        dbContext.ItineraryDays.SingleOrDefaultAsync(day => day.Id == dayId, cancellationToken);

    public Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken) =>
        dbContext.Products.AnyAsync(product => product.Id == productId, cancellationToken);

    public async Task<IReadOnlyList<ItineraryDay>> GetDaysAsync(
        int itineraryId,
        CancellationToken cancellationToken) =>
        await dbContext.ItineraryDays
            .Include(day => day.Activities)
            .Where(day => day.ItineraryId == itineraryId)
            .OrderBy(day => day.DayNumber)
            .ThenBy(day => day.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItineraryActivity>> GetActivitiesAsync(
        int dayId,
        CancellationToken cancellationToken) =>
        await dbContext.ItineraryActivities
            .Where(activity => activity.ItineraryDayId == dayId)
            .OrderBy(activity => activity.DisplayOrder)
            .ThenBy(activity => activity.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> DestinationExistsAsync(int destinationId, CancellationToken cancellationToken) =>
        dbContext.Destinations.AnyAsync(
            destination => destination.Id == destinationId,
            cancellationToken);

    public void Add(Itinerary itinerary) => dbContext.Itineraries.Add(itinerary);

    public void Add(ItineraryDay day) => dbContext.ItineraryDays.Add(day);

    public void Add(ItineraryActivity activity) => dbContext.ItineraryActivities.Add(activity);

    public void Remove(ItineraryDay day) => dbContext.ItineraryDays.Remove(day);

    public void Remove(ItineraryActivity activity) => dbContext.ItineraryActivities.Remove(activity);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ItineraryConflictException(
                "The itinerary was changed by another request. Reload it and try again.");
        }
    }
}
