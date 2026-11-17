using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Itineraries;
using ARTravel.Application.DTOs.Itineraries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/itineraries")]
[Authorize(Policy = "StaffRead")]
public sealed class ItinerariesController(IItineraryService itineraryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedItineraries>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedItineraries>> GetItineraries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? productId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default) =>
        Ok(await itineraryService.ListAsync(
            page,
            pageSize,
            productId,
            isActive,
            search,
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ItineraryDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItineraryDetailResponse>> GetItineraryById(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await itineraryService.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ItineraryDetailResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ItineraryDetailResponse>> CreateItinerary(
        CreateItineraryRequest request,
        CancellationToken cancellationToken)
    {
        var itinerary = await itineraryService.CreateAsync(
            new CreateItineraryCommand(
                request.ProductId,
                request.Name,
                request.Description),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetItineraryById),
            new { id = itinerary.Itinerary.Id },
            itinerary);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ItineraryDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItineraryDetailResponse>> UpdateItinerary(
        int id,
        UpdateItineraryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await itineraryService.UpdateAsync(
            id,
            new UpdateItineraryCommand(
                request.ProductId,
                request.Name,
                request.Description),
            cancellationToken));

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ItineraryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItineraryResponse>> UpdateItineraryStatus(
        int id,
        SetContactStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await itineraryService.SetStatusAsync(
            id,
            request.IsActive ?? throw new ArgumentException("IsActive is required."),
            cancellationToken));

    [HttpPost("{id:int}/duplicate")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ItineraryDetailResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ItineraryDetailResponse>> DuplicateItinerary(
        int id,
        CancellationToken cancellationToken)
    {
        var itinerary = await itineraryService.DuplicateAsync(id, cancellationToken);
        return CreatedAtAction(
            nameof(GetItineraryById),
            new { id = itinerary.Itinerary.Id },
            itinerary);
    }

    [HttpGet("{id:int}/days")]
    [ProducesResponseType<IReadOnlyList<ItineraryDayResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItineraryDayResponse>>> GetItineraryDays(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await itineraryService.GetDaysAsync(id, cancellationToken));

    [HttpPost("{id:int}/days")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ItineraryDayResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ItineraryDayResponse>> AddItineraryDay(
        int id,
        CreateItineraryDayRequest request,
        CancellationToken cancellationToken)
    {
        var day = await itineraryService.AddDayAsync(
            id,
            new AddItineraryDayCommand(request.Title, request.Description),
            cancellationToken);
        return CreatedAtAction(nameof(GetItineraryDays), new { id }, day);
    }

    [HttpPut("{id:int}/days/{dayId:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ItineraryDayResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItineraryDayResponse>> UpdateItineraryDay(
        int id,
        int dayId,
        UpdateItineraryDayRequest request,
        CancellationToken cancellationToken) =>
        Ok(await itineraryService.UpdateDayAsync(
            id,
            dayId,
            new UpdateItineraryDayCommand(request.Title, request.Description),
            cancellationToken));

    [HttpDelete("{id:int}/days/{dayId:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveItineraryDay(
        int id,
        int dayId,
        CancellationToken cancellationToken)
    {
        await itineraryService.RemoveDayAsync(id, dayId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/days/reorder")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderItineraryDays(
        int id,
        ReorderItineraryDaysRequest request,
        CancellationToken cancellationToken)
    {
        await itineraryService.ReorderDaysAsync(
            id,
            request.DayIds ?? throw new ArgumentException("DayIds is required."),
            cancellationToken);
        return NoContent();
    }
}
