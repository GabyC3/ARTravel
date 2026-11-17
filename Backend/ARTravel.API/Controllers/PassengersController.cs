using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Contacts;
using ARTravel.Application.DTOs.Contacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/passengers")]
[Authorize(Policy = "StaffRead")]
public sealed class PassengersController(IContactService contactService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedPassengers>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedPassengers>> GetPassengers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await contactService.ListPassengersAsync(
            page,
            pageSize,
            search,
            isActive,
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<PassengerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PassengerResponse>> GetPassengerById(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await contactService.GetPassengerAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "PassengerManagement")]
    [ProducesResponseType<PassengerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PassengerResponse>> CreatePassenger(
        CreatePassengerRequest request,
        CancellationToken cancellationToken)
    {
        var passenger = await contactService.CreatePassengerAsync(
            new CreatePassengerCommand(
                request.FirstName,
                request.LastName,
                request.DocumentType,
                request.DocumentNumber,
                request.BirthDate,
                request.Nationality,
                request.Email,
                request.PhoneNumber,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.PassportExpirationDate,
                request.Notes,
                request.IsActive),
            cancellationToken);
        return CreatedAtAction(nameof(GetPassengerById), new { id = passenger.Id }, passenger);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "PassengerManagement")]
    [ProducesResponseType<PassengerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PassengerResponse>> UpdatePassenger(
        int id,
        UpdatePassengerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await contactService.UpdatePassengerAsync(
            id,
            new UpdatePassengerCommand(
                request.FirstName,
                request.LastName,
                request.DocumentType,
                request.DocumentNumber,
                request.BirthDate,
                request.Nationality,
                request.Email,
                request.PhoneNumber,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.PassportExpirationDate,
                request.Notes),
            cancellationToken));

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "PassengerManagement")]
    [ProducesResponseType<PassengerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PassengerResponse>> UpdatePassengerStatus(
        int id,
        SetContactStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await contactService.SetPassengerStatusAsync(
            id,
            request.IsActive ?? throw new ArgumentException("IsActive is required."),
            cancellationToken));

    [HttpGet("{id:int}/trips")]
    [ProducesResponseType<PagedTrips>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedTrips>> GetPassengerTrips(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await contactService.GetPassengerTripsAsync(id, page, pageSize, cancellationToken));
}
