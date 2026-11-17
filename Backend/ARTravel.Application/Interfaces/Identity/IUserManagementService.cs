using ARTravel.Application.DTOs.Identity;

namespace ARTravel.Application.Interfaces.Identity;

public interface IUserManagementService
{
    Task<PagedUsers> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    Task<UserSummary> GetAsync(int userId, CancellationToken cancellationToken);

    Task<UserSummary> UpdateAsync(
        int userId,
        UpdateUserCommand command,
        CancellationToken cancellationToken);

    Task<UserSummary> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleSummary>> GetRolesAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<UserSummary> SetActiveAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken);
}
