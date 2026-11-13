using ARTravel.Application.Identity.Abstractions;
using ARTravel.Application.Identity.Contracts;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.Identity.Services;

public sealed class UserManagementService(
    IIdentityStore store,
    IPasswordHasher passwordHasher) : IUserManagementService
{
    public async Task<PagedUsers> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1 ||
            pageSize is < 1 or > 100 ||
            ((long)page - 1) * pageSize > int.MaxValue)
        {
            throw new ArgumentException("Page must be positive and pageSize must be between 1 and 100.");
        }

        var skip = checked((page - 1) * pageSize);
        var users = await store.ListUsersAsync(skip, pageSize, cancellationToken);
        var summaries = users.Select(ToSummary).ToArray();

        return new PagedUsers(
            summaries,
            page,
            pageSize,
            await store.CountUsersAsync(cancellationToken));
    }

    public async Task<UserSummary> GetAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        return ToSummary(user);
    }

    public async Task<UserSummary> UpdateAsync(
        int userId,
        UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName) ||
            command.FirstName.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(command.LastName) ||
            command.LastName.Trim().Length > 100)
        {
            throw new ArgumentException("First name and last name are required.");
        }

        var user = await GetUserAsync(userId, cancellationToken);
        user.FirstName = command.FirstName.Trim();
        user.LastName = command.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber)
            ? null
            : command.PhoneNumber.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(cancellationToken);
        return ToSummary(user);
    }

    public async Task<UserSummary> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        AuthService.ValidateUserDetails(command.FirstName, command.LastName, command.Email);
        AuthService.ValidatePassword(command.Password);
        var email = command.Email.Trim().ToLowerInvariant();
        if (await store.FindUserByEmailAsync(email, cancellationToken) is not null)
        {
            throw new IdentityConflictException("An account with this email already exists.");
        }

        var role = await store.FindRoleByNormalizedNameAsync("USER", cancellationToken)
            ?? throw new IdentityConfigurationException(
                "The default User role is missing. Apply the database migrations.");
        var user = new User
        {
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(command.Password),
            PhoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber)
                ? null
                : command.PhoneNumber.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        store.Add(user);
        store.Add(new UserRole { User = user, Role = role });
        await store.SaveChangesAsync(cancellationToken);
        return ToSummary(user) with { Roles = [role.Name] };
    }

    public async Task<IReadOnlyList<RoleSummary>> GetRolesAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        return user.UserRoles
            .Select(userRole => userRole.Role)
            .OrderBy(role => role.Name)
            .Select(role => new RoleSummary(
                role.Id,
                role.Name,
                role.Description,
                role.IsActive,
                role.CreatedAt))
            .ToArray();
    }

    public async Task<UserSummary> SetActiveAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        if (user.IsActive && !isActive)
        {
            foreach (var role in user.UserRoles
                         .Select(userRole => userRole.Role)
                         .Where(role => role.NormalizedName == "ADMIN"))
            {
                if (await store.CountActiveUsersWithRoleAsync(role.Id, cancellationToken) <= 1)
                {
                    throw new IdentityConflictException(
                        "The last active administrator cannot be deactivated.");
                }
            }
        }

        await store.SetUserActiveAndInvalidateSessionsAsync(
            userId,
            isActive,
            cancellationToken);
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.TokenVersion++;
        return ToSummary(user);
    }

    private async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken) =>
        await store.FindUserByIdAsync(userId, cancellationToken)
        ?? throw new IdentityNotFoundException($"User {userId} was not found.");

    private static UserSummary ToSummary(User user) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            user.UserRoles
                .Select(userRole => userRole.Role)
                .Where(role => role.IsActive)
                .Select(role => role.Name)
                .ToArray());
}
