using ARTravel.Application.Identity.Abstractions;
using ARTravel.Application.Identity.Contracts;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.Identity.Services;

public sealed class RoleManagementService(IIdentityStore store) : IRoleManagementService
{
    public async Task<IReadOnlyList<RoleSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var roles = await store.ListRolesAsync(cancellationToken);
        return roles.Select(ToSummary).ToArray();
    }

    public async Task<RoleSummary> CreateAsync(
        CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        if (name.Length is < 2 or > 50)
        {
            throw new ArgumentException("Role name must be between 2 and 50 characters.");
        }

        var normalizedName = name.ToUpperInvariant();
        if (await store.FindRoleByNormalizedNameAsync(normalizedName, cancellationToken) is not null)
        {
            throw new IdentityConflictException("A role with this name already exists.");
        }

        var role = new Role
        {
            Name = name,
            NormalizedName = normalizedName,
            Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        store.Add(role);
        await store.SaveChangesAsync(cancellationToken);
        return ToSummary(role);
    }

    public async Task<RoleSummary> GetAsync(int roleId, CancellationToken cancellationToken)
    {
        var role = await store.FindRoleByIdAsync(roleId, cancellationToken)
            ?? throw new IdentityNotFoundException($"Role {roleId} was not found.");
        return ToSummary(role);
    }

    public async Task<RoleSummary> UpdateAsync(
        int roleId,
        UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var role = await store.FindRoleByIdAsync(roleId, cancellationToken)
            ?? throw new IdentityNotFoundException($"Role {roleId} was not found.");
        var name = command.Name.Trim();
        if (name.Length is < 2 or > 50)
        {
            throw new ArgumentException("Role name must be between 2 and 50 characters.");
        }

        var normalizedName = name.ToUpperInvariant();
        if (role.NormalizedName != normalizedName &&
            await store.FindRoleByNormalizedNameAsync(normalizedName, cancellationToken) is not null)
        {
            throw new IdentityConflictException("A role with this name already exists.");
        }

        if ((role.NormalizedName is "ADMIN" or "USER") &&
            role.NormalizedName != normalizedName)
        {
            throw new IdentityConflictException("System roles cannot be renamed.");
        }

        role.Name = name;
        role.NormalizedName = normalizedName;
        role.Description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();
        await store.SaveChangesAsync(cancellationToken);
        return ToSummary(role);
    }

    public async Task<RoleSummary> SetActiveAsync(
        int roleId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var role = await store.FindRoleByIdAsync(roleId, cancellationToken)
            ?? throw new IdentityNotFoundException($"Role {roleId} was not found.");
        if (role.IsActive &&
            !isActive &&
            (role.NormalizedName is "ADMIN" or "USER"))
        {
            throw new IdentityConflictException("System roles cannot be deactivated.");
        }

        role.IsActive = isActive;
        await store.SaveChangesAsync(cancellationToken);
        return ToSummary(role);
    }

    public async Task DeleteAsync(int roleId, CancellationToken cancellationToken)
    {
        var role = await store.FindRoleByIdAsync(roleId, cancellationToken)
            ?? throw new IdentityNotFoundException($"Role {roleId} was not found.");
        if (role.NormalizedName is "ADMIN" or "USER")
        {
            throw new IdentityConflictException("System roles cannot be deleted.");
        }

        if (await store.CountUsersWithRoleAsync(roleId, cancellationToken) != 0)
        {
            throw new IdentityConflictException("A role assigned to users cannot be deleted.");
        }

        store.Remove(role);
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken)
    {
        var user = await store.FindUserByIdAsync(userId, cancellationToken)
            ?? throw new IdentityNotFoundException($"User {userId} was not found.");
        if (!user.IsActive)
        {
            throw new IdentityConflictException("Roles cannot be assigned to an inactive user.");
        }

        var role = await store.FindRoleByIdAsync(roleId, cancellationToken)
            ?? throw new IdentityNotFoundException($"Role {roleId} was not found.");
        if (!role.IsActive)
        {
            throw new IdentityConflictException("An inactive role cannot be assigned.");
        }

        if (await store.FindUserRoleAsync(userId, roleId, cancellationToken) is null)
        {
            store.Add(new UserRole { User = user, UserId = userId, Role = role, RoleId = roleId });
            await store.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken)
    {
        var userRole = await store.FindUserRoleAsync(userId, roleId, cancellationToken)
            ?? throw new IdentityNotFoundException(
                $"User {userId} does not have role {roleId}.");

        if (userRole.Role.NormalizedName == "ADMIN" &&
            userRole.User.IsActive &&
            await store.CountActiveUsersWithRoleAsync(roleId, cancellationToken) <= 1)
        {
            throw new IdentityConflictException(
                "The last active administrator role assignment cannot be removed.");
        }

        store.Remove(userRole);
        await store.SaveChangesAsync(cancellationToken);
    }

    private static RoleSummary ToSummary(Role role) =>
        new(role.Id, role.Name, role.Description, role.IsActive, role.CreatedAt);
}
