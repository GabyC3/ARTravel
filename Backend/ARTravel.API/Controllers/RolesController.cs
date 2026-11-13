using ARTravel.API.Contracts;
using ARTravel.Application.Identity.Abstractions;
using ARTravel.Application.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTravel.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "Administrator")]
public sealed class RolesController(IRoleManagementService roleManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RoleSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleSummary>>> List(
        CancellationToken cancellationToken)
    {
        return Ok(await roleManagementService.ListAsync(cancellationToken));
    }

    [HttpGet("{roleId:int}")]
    [ProducesResponseType<RoleSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleSummary>> Get(
        int roleId,
        CancellationToken cancellationToken)
    {
        return Ok(await roleManagementService.GetAsync(roleId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<RoleSummary>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleSummary>> Create(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await roleManagementService.CreateAsync(
            new CreateRoleCommand(request.Name, request.Description),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { roleId = role.Id }, role);
    }

    [HttpPut("{roleId:int}")]
    [ProducesResponseType<RoleSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleSummary>> Update(
        int roleId,
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await roleManagementService.UpdateAsync(
            roleId,
            new UpdateRoleCommand(request.Name, request.Description),
            cancellationToken);
        return Ok(role);
    }

    [HttpPatch("{roleId:int}/status")]
    [ProducesResponseType<RoleSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RoleSummary>> SetActive(
        int roleId,
        SetRoleActiveRequest request,
        CancellationToken cancellationToken)
    {
        var role = await roleManagementService.SetActiveAsync(
            roleId,
            request.IsActive ?? throw new ArgumentException("IsActive is required."),
            cancellationToken);
        return Ok(role);
    }

    [HttpDelete("{roleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        int roleId,
        CancellationToken cancellationToken)
    {
        await roleManagementService.DeleteAsync(roleId, cancellationToken);
        return NoContent();
    }

    [HttpPost("~/api/users/{userId:int}/roles/{roleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Assign(
        int userId,
        int roleId,
        CancellationToken cancellationToken)
    {
        await roleManagementService.AssignAsync(userId, roleId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("~/api/users/{userId:int}/roles/{roleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(
        int userId,
        int roleId,
        CancellationToken cancellationToken)
    {
        await roleManagementService.RemoveAsync(userId, roleId, cancellationToken);
        return NoContent();
    }
}
