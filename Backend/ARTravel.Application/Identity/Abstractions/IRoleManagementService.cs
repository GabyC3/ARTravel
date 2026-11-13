using ARTravel.Application.Identity.Contracts;

namespace ARTravel.Application.Identity.Abstractions;

public interface IRoleManagementService
{
    Task<IReadOnlyList<RoleSummary>> ListAsync(CancellationToken cancellationToken);

    Task<RoleSummary> CreateAsync(
        CreateRoleCommand command,
        CancellationToken cancellationToken);

    Task<RoleSummary> GetAsync(int roleId, CancellationToken cancellationToken);

    Task<RoleSummary> UpdateAsync(
        int roleId,
        UpdateRoleCommand command,
        CancellationToken cancellationToken);

    Task<RoleSummary> SetActiveAsync(
        int roleId,
        bool isActive,
        CancellationToken cancellationToken);

    Task DeleteAsync(int roleId, CancellationToken cancellationToken);

    Task AssignAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken);
}
