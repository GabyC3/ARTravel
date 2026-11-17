using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Departures;
using ARTravel.Application.DTOs.Departures;
using ARTravel.Application.Interfaces.Products;
using ARTravel.Application.DTOs.Products;
using ARTravel.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = "StaffRead")]
public sealed class ProductsController(
    IProductService productService,
    IDepartureService departureService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedProducts>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedProducts>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] ProductType? productType = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default) =>
        Ok(await productService.ListAsync(
            page,
            pageSize,
            search,
            productType,
            isPublished,
            isActive,
            cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDetail>> GetProductById(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ProductDetail>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDetail>> CreateProduct(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(
            new CreateProductCommand(
                request.Name,
                request.Code,
                request.Description,
                request.ProductType,
                request.DurationDays,
                request.DurationNights,
                request.BasePrice,
                request.Currency,
                request.MinimumPassengers,
                request.MaximumPassengers,
                request.Includes,
                request.Excludes,
                request.Recommendations,
                request.CancellationPolicy,
                request.IsPublished,
                request.IsActive),
            cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = product.Product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ProductDetail>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDetail>> UpdateProduct(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        Ok(await productService.UpdateAsync(
            id,
            new UpdateProductCommand(
                request.Name,
                request.Code,
                request.Description,
                request.ProductType,
                request.DurationDays,
                request.DurationNights,
                request.BasePrice,
                request.Currency,
                request.MinimumPassengers,
                request.MaximumPassengers,
                request.Includes,
                request.Excludes,
                request.Recommendations,
                request.CancellationPolicy),
            cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProduct(
        int id,
        CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<TourismProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TourismProductResponse>> UpdateProductStatus(
        int id,
        SetContactStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await productService.SetStatusAsync(
            id,
            request.IsActive ?? throw new ArgumentException("IsActive is required."),
            cancellationToken));

    [HttpPatch("{id:int}/publication")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<TourismProductResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TourismProductResponse>> UpdateProductPublication(
        int id,
        SetProductPublicationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await productService.SetPublicationAsync(
            id,
            request.IsPublished ?? throw new ArgumentException("IsPublished is required."),
            cancellationToken));

    [HttpGet("{id:int}/destinations")]
    [ProducesResponseType<IReadOnlyList<ProductDestinationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDestinationResponse>>> GetProductDestinations(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetDestinationsAsync(id, cancellationToken));

    [HttpPost("{id:int}/destinations")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<IReadOnlyList<ProductDestinationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDestinationResponse>>> AssignProductDestinations(
        int id,
        AssignProductDestinationsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await productService.AssignDestinationsAsync(
            id,
            (request.Destinations ?? throw new ArgumentException("Destinations is required."))
                .Select(destination => new AssignProductDestinationItem(
                    destination.DestinationId,
                    destination.VisitOrder,
                    destination.IsMainDestination,
                    destination.Notes))
                .ToArray(),
            cancellationToken));

    [HttpDelete("{id:int}/destinations/{destinationId:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveProductDestination(
        int id,
        int destinationId,
        CancellationToken cancellationToken)
    {
        await productService.RemoveDestinationAsync(id, destinationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/images")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ProductImageResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductImageResponse>> AddProductImage(
        int id,
        AddProductImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await productService.AddImageAsync(
            id,
            request.ImageUrl,
            request.AltText,
            request.IsPrimary,
            request.DisplayOrder,
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, image);
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveProductImage(
        int id,
        int imageId,
        CancellationToken cancellationToken)
    {
        await productService.RemoveImageAsync(id, imageId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:int}/images/{imageId:int}/primary")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<ProductImageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductImageResponse>> SetPrimaryProductImage(
        int id,
        int imageId,
        CancellationToken cancellationToken) =>
        Ok(await productService.SetPrimaryImageAsync(id, imageId, cancellationToken));

    [HttpGet("{id:int}/departures")]
    [ProducesResponseType<PagedDepartures>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedDepartures>> GetProductDepartures(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await departureService.GetProductDeparturesAsync(
            id,
            page,
            pageSize,
            cancellationToken));

    [HttpPost("{id:int}/departures")]
    [Authorize(Policy = "Manager")]
    [ProducesResponseType<DepartureResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartureResponse>> CreateProductDeparture(
        int id,
        CreateProductDepartureRequest request,
        CancellationToken cancellationToken)
    {
        var departure = await departureService.CreateForProductAsync(
            id,
            new CreateDepartureCommand(
                id,
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
                request.Notes),
            cancellationToken);
        return Created($"/api/departures/{departure.Id}", departure);
    }
}
