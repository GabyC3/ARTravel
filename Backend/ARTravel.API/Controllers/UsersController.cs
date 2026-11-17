using ARTravel.API.Contracts;
using ARTravel.Application.Interfaces.Identity;
using ARTravel.Application.DTOs.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "Manager")]
public sealed class UsersController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedUsers>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedUsers>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await userManagementService.ListAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<UserSummary>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserSummary>> Create(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManagementService.CreateAsync(
            new CreateUserCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                request.PhoneNumber),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { userId = user.Id }, user);
    }

    [HttpGet("{userId:int}")]
    [ProducesResponseType<UserSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummary>> Get(
        int userId,
        CancellationToken cancellationToken)
    {
        return Ok(await userManagementService.GetAsync(userId, cancellationToken));
    }

    [HttpGet("{userId:int}/roles")]
    [ProducesResponseType<IReadOnlyList<RoleSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleSummary>>> GetRoles(
        int userId,
        CancellationToken cancellationToken)
    {
        return Ok(await userManagementService.GetRolesAsync(userId, cancellationToken));
    }

    [HttpPut("{userId:int}")]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<UserSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummary>> Update(
        int userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await userManagementService.UpdateAsync(
            userId,
            new UpdateUserCommand(request.FirstName, request.LastName, request.PhoneNumber),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{userId:int}/status")]
    [Authorize(Policy = "Administrator")]
    [ProducesResponseType<UserSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSummary>> SetActive(
        int userId,
        SetUserActiveRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await userManagementService.SetActiveAsync(
            userId,
            request.IsActive ?? throw new ArgumentException("IsActive is required."),
            cancellationToken);
        return Ok(updated);
    }
}
