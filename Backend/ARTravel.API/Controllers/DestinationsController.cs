using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Destinations;
using ARTravel.Application.DTOs.Destinations;
using ARTravel.Application.DTOs.Products;
using ARTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/destinations")]
[Authorize(Policy = "StaffRead")]
public sealed class DestinationsController(IDestinationService destinationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedDestinations>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDestinations>> GetDestinations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DestinationType? destinationType = null,
        [FromQuery] string? country = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await destinationService.ListAsync(
            page,
            pageSize,
            search,
            destinationType,
            country,
            isActive,
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<DestinationDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DestinationDetail>> GetDestinationById(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await destinationService.GetAsync(id, cancellationToken));

    [HttpGet("parents")]
    [ProducesResponseType<IReadOnlyList<DestinationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DestinationResponse>>> GetParentDestinations(
        CancellationToken cancellationToken) =>
        Ok(await destinationService.GetParentDestinationsAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<DestinationDetail>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DestinationDetail>> CreateDestination(
        CreateDestinationRequest request,
        CancellationToken cancellationToken)
    {
        var destination = await destinationService.CreateAsync(
            new CreateDestinationCommand(
                request.Name,
                request.Description,
                request.DestinationType,
                request.Country,
                request.Province,
                request.City,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.AltitudeMeters,
                request.ParentDestinationId,
                request.IsActive,
                request.ImageUrls),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetDestinationById),
            new { id = destination.Destination.Id },
            destination);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<DestinationDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DestinationDetail>> UpdateDestination(
        int id,
        UpdateDestinationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await destinationService.UpdateAsync(
            id,
            new UpdateDestinationCommand(
                request.Name,
                request.Description,
                request.DestinationType,
                request.Country,
                request.Province,
                request.City,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.AltitudeMeters,
                request.ParentDestinationId),
            cancellationToken));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDestination(
        int id,
        CancellationToken cancellationToken)
    {
        await destinationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<DestinationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DestinationResponse>> UpdateDestinationStatus(
        int id,
        SetContactStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await destinationService.SetStatusAsync(
            id,
            request.IsActive ?? throw new ArgumentException("IsActive is required."),
            cancellationToken));

    [HttpGet("{id:int}/children")]
    [ProducesResponseType<PagedDestinations>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDestinations>> GetChildDestinations(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await destinationService.GetChildrenAsync(id, page, pageSize, cancellationToken));

    [HttpGet("{id:int}/products")]
    [ProducesResponseType<PagedProducts>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedProducts>> GetDestinationProducts(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await destinationService.GetProductsAsync(
            id,
            page,
            pageSize,
            isActive,
            cancellationToken));

    [HttpGet("{id:int}/summary")]
    [ProducesResponseType<DestinationSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DestinationSummary>> GetDestinationSummary(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await destinationService.GetSummaryAsync(id, cancellationToken));

    [HttpPost("{id:int}/images")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<DestinationImageResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DestinationImageResponse>> AddDestinationImage(
        int id,
        AddDestinationImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await destinationService.AddImageAsync(
            id,
            new AddDestinationImageCommand(
                request.ImageUrl,
                request.AltText,
                request.IsPrimary,
                request.DisplayOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetDestinationById), new { id }, image);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveDestinationImage(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        await destinationService.RemoveImageAsync(id, imageId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/images/{imageId:int}/primary")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<DestinationImageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DestinationImageResponse>> SetPrimaryDestinationImage(
        int id,
        int imageId,
        CancellationToken cancellationToken) =>
        Ok(await destinationService.SetPrimaryImageAsync(id, imageId, cancellationToken));
}
