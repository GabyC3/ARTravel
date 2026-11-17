using System.Security.Claims;
using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Reservations;
using ARTravel.Application.DTOs.Reservations;
using ARTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize(Policy = "StaffRead")]
public sealed class ReservationsController(IReservationService reservationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedReservations>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedReservations>> GetReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? customerId = null,
        [FromQuery] int? departureId = null,
        [FromQuery] ReservationStatus? status = null,
        [FromQuery] DateTime? reservationDateFrom = null,
        [FromQuery] DateTime? reservationDateTo = null,
        CancellationToken cancellationToken = default) =>
        Ok(await reservationService.ListAsync(
            page,
            pageSize,
            search,
            customerId,
            departureId,
            status,
            reservationDateFrom,
            reservationDateTo,
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ReservationDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationDetail>> GetReservationById(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await reservationService.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType<ReservationDetail>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReservationDetail>> CreateReservation(
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var reservation = await reservationService.CreateAsync(
            new CreateReservationCommand(
                request.CustomerId,
                request.DepartureId,
                request.Notes,
                userId),
            cancellationToken);
        return CreatedAtAction(nameof(GetReservationById), new { id = reservation.Reservation.Id }, reservation);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType<ReservationDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationDetail>> UpdateReservation(
        int id,
        UpdateReservationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await reservationService.UpdateAsync(
            id,
            new UpdateReservationCommand(request.Notes, request.Discount),
            cancellationToken));

    [HttpPatch("{id:int}/confirm")]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType<ReservationDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationDetail>> ConfirmReservation(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await reservationService.ConfirmAsync(id, cancellationToken));

    [HttpPatch("{id:int}/cancel")]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType<ReservationDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationDetail>> CancelReservation(
        int id,
        CancelReservationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await reservationService.CancelAsync(
            id,
            new CancelReservationCommand(request.Reason, request.RefundAmount, userId),
            cancellationToken));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType<ReservationDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationDetail>> UpdateReservationStatus(
        int id,
        SetReservationStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await reservationService.SetStatusAsync(
            id,
            new SetReservationStatusCommand(
                request.Status ?? throw new ArgumentException("Status is required.")),
            cancellationToken));

    [HttpGet("{id:int}/payments")]
    [ProducesResponseType<IReadOnlyList<ReservationPaymentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationPaymentResponse>>> GetReservationPayments(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await reservationService.GetPaymentsAsync(id, cancellationToken));

    [HttpGet("{reservationId:int}/passengers")]
    [ProducesResponseType<PagedReservationPassengerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedReservationPassengerResponse>> GetReservationPassengers(
        int reservationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await reservationService.ListPassengersAsync(
            reservationId,
            page,
            pageSize,
            cancellationToken);
        return Ok(new PagedReservationPassengerResponse(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount));
    }

    [HttpGet("{reservationId:int}/passengers/{passengerId:int}")]
    [ProducesResponseType<ReservationPassengerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationPassengerResponse>> GetReservationPassenger(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken) =>
        Ok(await reservationService.GetPassengerAsync(
            reservationId,
            passengerId,
            cancellationToken));

    [HttpPost("{reservationId:int}/passengers")]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType<ReservationPassengerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReservationPassengerResponse>> AddReservationPassenger(
        int reservationId,
        CreateReservationPassengerRequest request,
        CancellationToken cancellationToken)
    {
        var passenger = await reservationService.AddPassengerAsync(
            reservationId,
            ToCreateCommand(request),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetReservationPassenger),
            new { reservationId, passengerId = passenger.Id },
            passenger);
    }

    [HttpPut("{reservationId:int}/passengers/{passengerId:int}")]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType<ReservationPassengerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationPassengerResponse>> UpdateReservationPassenger(
        int reservationId,
        int passengerId,
        UpdateReservationPassengerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await reservationService.UpdatePassengerAsync(
            reservationId,
            passengerId,
            ToUpdateCommand(request),
            cancellationToken));

    [HttpDelete("{reservationId:int}/passengers/{passengerId:int}")]
    [Authorize(Policy = "Commercial")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveReservationPassenger(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken)
    {
        await reservationService.RemovePassengerAsync(
            reservationId,
            passengerId,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("~/api/departures/{departureId:int}/reservations")]
    [ProducesResponseType<PagedReservations>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedReservations>> GetDepartureReservations(
        int departureId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await reservationService.ListByDepartureAsync(
            departureId,
            page,
            pageSize,
            cancellationToken));

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    private static CreateReservationPassengerCommand ToCreateCommand(
        CreateReservationPassengerRequest request) =>
        new(
            request.PassengerId,
            request.FirstName,
            request.LastName,
            request.DocumentType ?? throw new ArgumentException("DocumentType is required."),
            request.DocumentNumber,
            request.BirthDate,
            request.Gender,
            request.PhoneNumber,
            request.Email,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.SpecialRequirements,
            request.Notes,
            request.PriceType);

    private static UpdateReservationPassengerCommand ToUpdateCommand(
        UpdateReservationPassengerRequest request) =>
        new(
            request.PassengerId,
            request.FirstName,
            request.LastName,
            request.DocumentType ?? throw new ArgumentException("DocumentType is required."),
            request.DocumentNumber,
            request.BirthDate,
            request.Gender,
            request.PhoneNumber,
            request.Email,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.SpecialRequirements,
            request.Notes,
            request.PriceType);
}

public sealed record PagedReservationPassengerResponse(
    IReadOnlyList<ReservationPassengerResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
