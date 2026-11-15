using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Itineraries;
using ARTravel.Application.DTOs.Itineraries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/itinerary-days/{dayId:int}/activities")]
[Authorize(Policy = "Administrator")]
public sealed class ItineraryDaysController(IItineraryService itineraryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ItineraryActivityResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItineraryActivityResponse>>> GetItineraryActivities(
        int dayId,
        CancellationToken cancellationToken) =>
        Ok(await itineraryService.GetActivitiesAsync(dayId, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ItineraryActivityResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ItineraryActivityResponse>> AddItineraryActivity(
        int dayId,
        AddItineraryActivityRequest request,
        CancellationToken cancellationToken)
    {
        var activity = await itineraryService.AddActivityAsync(
            dayId,
            new AddItineraryActivityCommand(
                request.DestinationId,
                request.Name,
                request.Description,
                request.ActivityType,
                request.StartTime,
                request.EndTime,
                request.DurationMinutes,
                request.DisplayOrder,
                request.Notes),
            cancellationToken);
        return CreatedAtAction(nameof(GetItineraryActivities), new { dayId }, activity);
    }

    [HttpPut("{activityId:int}")]
    [ProducesResponseType<ItineraryActivityResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItineraryActivityResponse>> UpdateItineraryActivity(
        int dayId,
        int activityId,
        UpdateItineraryActivityRequest request,
        CancellationToken cancellationToken) =>
        Ok(await itineraryService.UpdateActivityAsync(
            dayId,
            activityId,
            new UpdateItineraryActivityCommand(
                request.DestinationId,
                request.Name,
                request.Description,
                request.ActivityType,
                request.StartTime,
                request.EndTime,
                request.DurationMinutes,
                request.Notes),
            cancellationToken));

    [HttpDelete("{activityId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveItineraryActivity(
        int dayId,
        int activityId,
        CancellationToken cancellationToken)
    {
        await itineraryService.RemoveActivityAsync(dayId, activityId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderItineraryActivities(
        int dayId,
        ReorderItineraryActivitiesRequest request,
        CancellationToken cancellationToken)
    {
        await itineraryService.ReorderActivitiesAsync(
            dayId,
            request.ActivityIds ?? throw new ArgumentException("ActivityIds is required."),
            cancellationToken);
        return NoContent();
    }
}
