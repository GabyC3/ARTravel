using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Departures;
using ARTravel.Application.DTOs.Departures;
using ARTravel.Application.Interfaces.Reservations;
using ARTravel.Application.DTOs.Reservations;
using ARTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/departures")]
[Authorize(Policy = "Administrator")]
public sealed class DeparturesController(
    IDepartureService departureService,
    IReservationService reservationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedDepartures>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDepartures>> GetDepartures(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? productId = null,
        [FromQuery] DepartureStatus? status = null,
        CancellationToken cancellationToken = default) =>
        Ok(await departureService.ListAsync(
            page,
            pageSize,
            productId,
            status,
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<DepartureResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartureResponse>> GetDepartureById(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await departureService.GetAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<DepartureResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartureResponse>> CreateDeparture(
        CreateDepartureRequest request,
        CancellationToken cancellationToken)
    {
        var departure = await departureService.CreateAsync(ToCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetDepartureById), new { id = departure.Id }, departure);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<DepartureResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartureResponse>> UpdateDeparture(
        int id,
        UpdateDepartureRequest request,
        CancellationToken cancellationToken) =>
        Ok(await departureService.UpdateAsync(
            id,
            new UpdateDepartureCommand(
                request.ItineraryId,
                request.DepartureDate,
                request.ReturnDate,
                request.MeetingPoint,
                request.MeetingTime,
                request.Capacity,
                request.MinimumPassengers,
                request.BasePrice,
                request.Currency,
                request.Notes),
            cancellationToken));

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<DepartureResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartureResponse>> UpdateDepartureStatus(
        int id,
        SetDepartureStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await departureService.SetStatusAsync(
            id,
            request.Status ?? throw new ArgumentException("Status is required."),
            cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<DepartureResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartureResponse>> CancelDeparture(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await departureService.CancelAsync(id, cancellationToken));

    [HttpGet("{id:int}/availability")]
    [ProducesResponseType<DepartureAvailability>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartureAvailability>> GetDepartureAvailability(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await departureService.GetAvailabilityAsync(id, cancellationToken));

    [HttpGet("{id:int}/passengers")]
    [ProducesResponseType<PagedReservationPassengers>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedReservationPassengers>> GetDeparturePassengers(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await reservationService.ListDeparturePassengersAsync(
            id,
            page,
            pageSize,
            cancellationToken));

    [HttpGet("{id:int}/prices")]
    [ProducesResponseType<IReadOnlyList<DeparturePriceResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeparturePriceResponse>>> GetDeparturePrices(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await departureService.GetPricesAsync(id, cancellationToken));

    [HttpPost("{id:int}/prices")]
    [ProducesResponseType<DeparturePriceResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DeparturePriceResponse>> AddDeparturePrice(
        int id,
        AddDeparturePriceRequest request,
        CancellationToken cancellationToken)
    {
        var price = await departureService.AddPriceAsync(
            id,
            ToCommand(request),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, price);
    }

    [HttpPut("{id:int}/prices/{priceId:int}")]
    [ProducesResponseType<DeparturePriceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DeparturePriceResponse>> UpdateDeparturePrice(
        int id,
        int priceId,
        AddDeparturePriceRequest request,
        CancellationToken cancellationToken) =>
        Ok(await departureService.UpdatePriceAsync(
            id,
            priceId,
            ToCommand(request),
            cancellationToken));

    [HttpDelete("{id:int}/prices/{priceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveDeparturePrice(
        int id,
        int priceId,
        CancellationToken cancellationToken)
    {
        await departureService.RemovePriceAsync(id, priceId, cancellationToken);
        return NoContent();
    }

    private static CreateDepartureCommand ToCommand(CreateDepartureRequest request) =>
        new(
            request.ProductId,
            request.ItineraryId,
            request.DepartureDate,
            request.ReturnDate,
            request.MeetingPoint,
            request.MeetingTime,
            request.Capacity,
            request.MinimumPassengers,
            request.BasePrice,
            request.Currency,
            request.Status,
            request.Notes);

    private static CreateDeparturePriceCommand ToCommand(AddDeparturePriceRequest request) =>
        new(
            request.PriceType ?? throw new ArgumentException("PriceType is required."),
            request.Amount,
            request.Currency,
            request.Description);
}
