using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Contacts;
using ARTravel.Application.DTOs.Contacts;
using ARTravel.Application.Interfaces.Reservations;
using ARTravel.Application.DTOs.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Policy = "CustomerRead")]
public sealed class CustomersController(
    IContactService contactService,
    IReservationService reservationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedCustomers>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedCustomers>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await contactService.ListCustomersAsync(
            page,
            pageSize,
            search,
            isActive,
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> GetCustomerById(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await contactService.GetCustomerAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "CustomerManagement")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await contactService.CreateCustomerAsync(
            new CreateCustomerCommand(
                request.CustomerType,
                request.FirstName,
                request.LastName,
                request.BusinessName,
                request.DocumentType,
                request.DocumentNumber,
                request.Email,
                request.PhoneNumber,
                request.Address,
                request.City,
                request.Country,
                request.Notes,
                request.IsActive),
            cancellationToken);
        return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CustomerManagement")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await contactService.UpdateCustomerAsync(
            id,
            new UpdateCustomerCommand(
                request.CustomerType,
                request.FirstName,
                request.LastName,
                request.BusinessName,
                request.DocumentType,
                request.DocumentNumber,
                request.Email,
                request.PhoneNumber,
                request.Address,
                request.City,
                request.Country,
                request.Notes),
            cancellationToken));

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "CustomerManagement")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomerStatus(
        int id,
        SetContactStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await contactService.SetCustomerStatusAsync(
            id,
            request.IsActive ?? throw new ArgumentException("IsActive is required."),
            cancellationToken));

    [HttpGet("{id:int}/reservations")]
    [ProducesResponseType<PagedReservations>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedReservations>> GetCustomerReservations(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await reservationService.ListByCustomerAsync(id, page, pageSize, cancellationToken));

    [HttpGet("{id:int}/summary")]
    [ProducesResponseType<CustomerSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerSummary>> GetCustomerSummary(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await contactService.GetCustomerSummaryAsync(id, cancellationToken));
}
