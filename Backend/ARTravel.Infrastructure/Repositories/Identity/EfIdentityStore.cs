using ARTravel.Application.Interfaces.Identity;
using ARTravel.Domain.Entities;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ARTravel.Infrastructure.Repositories.Identity;

public sealed class EfIdentityStore(AppDbContext dbContext) : IIdentityStore
{
    public Task<User?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == email,
            cancellationToken);

    public Task<User?> FindUserByIdAsync(int userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> ListUsersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .OrderBy(user => user.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountUsersAsync(CancellationToken cancellationToken) =>
        dbContext.Users.CountAsync(cancellationToken);

    public Task<Role?> FindRoleByIdAsync(int roleId, CancellationToken cancellationToken) =>
        dbContext.Roles.SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    public Task<Role?> FindRoleByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken) =>
        dbContext.Roles.SingleOrDefaultAsync(
            role => role.NormalizedName == normalizedName,
            cancellationToken);

    public async Task<IReadOnlyList<Role>> ListRolesAsync(
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        await dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ThenBy(role => role.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountRolesAsync(CancellationToken cancellationToken) =>
        dbContext.Roles.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Role>> GetUserRolesAsync(
        int userId,
        CancellationToken cancellationToken) =>
        await dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.Role)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<UserRole?> FindUserRoleAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken) =>
        dbContext.UserRoles
            .Include(userRole => userRole.User)
            .Include(userRole => userRole.Role)
            .SingleOrDefaultAsync(
                userRole => userRole.UserId == userId && userRole.RoleId == roleId,
                cancellationToken);

    public Task<int> CountActiveUsersWithRoleAsync(
        int roleId,
        CancellationToken cancellationToken) =>
        dbContext.UserRoles.CountAsync(
            userRole => userRole.RoleId == roleId && userRole.User.IsActive,
            cancellationToken);

    public Task<int> CountUsersWithRoleAsync(int roleId, CancellationToken cancellationToken) =>
        dbContext.UserRoles.CountAsync(
            userRole => userRole.RoleId == roleId,
            cancellationToken);

    public Task<bool> IsAccessTokenRevokedAsync(
        string tokenId,
        CancellationToken cancellationToken) =>
        dbContext.RevokedAccessTokens.AnyAsync(
            token => token.TokenId == tokenId && token.ExpiresAt > DateTime.UtcNow,
            cancellationToken);

    public Task<RefreshToken?> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> RotateRefreshTokenAsync(
        string currentTokenHash,
        RefreshToken replacement,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var revokedCount = await dbContext.RefreshTokens
            .Where(token =>
                token.TokenHash == currentTokenHash &&
                token.RevokedAt == null &&
                token.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);

        if (revokedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.RefreshTokens.Add(replacement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RevokeSessionRefreshTokensAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(token => token.SessionId == sessionId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);
    }

    public async Task<bool> ConsumePasswordResetTokenAsync(
        string tokenHash,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var consumedCount = await dbContext.PasswordResetTokens
            .Where(token =>
                token.TokenHash == tokenHash &&
                token.UsedAt == null &&
                token.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.UsedAt, now),
                cancellationToken);

        if (consumedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var userId = await dbContext.PasswordResetTokens
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => token.UserId)
            .SingleAsync(cancellationToken);

        await dbContext.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.PasswordHash, passwordHash)
                    .SetProperty(user => user.TokenVersion, user => user.TokenVersion + 1)
                    .SetProperty(user => user.UpdatedAt, now),
                cancellationToken);
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task UpdatePasswordAndRevokeSessionsAsync(
        int userId,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updatedCount = await dbContext.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.PasswordHash, passwordHash)
                    .SetProperty(user => user.TokenVersion, user => user.TokenVersion + 1)
                    .SetProperty(user => user.UpdatedAt, now),
                cancellationToken);
        if (updatedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("The user account could not be updated.");
        }

        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetUserActiveAndInvalidateSessionsAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var updatedCount = await dbContext.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.IsActive, isActive)
                    .SetProperty(user => user.TokenVersion, user => user.TokenVersion + 1)
                    .SetProperty(user => user.UpdatedAt, now),
                cancellationToken);
        if (updatedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("The user account could not be updated.");
        }

        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.RevokedAt, now),
                cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public void Add(User user) => dbContext.Users.Add(user);

    public void Add(Role role) => dbContext.Roles.Add(role);

    public void Add(UserRole userRole) => dbContext.UserRoles.Add(userRole);

    public void Add(RevokedAccessToken token) => dbContext.RevokedAccessTokens.Add(token);

    public void Add(RefreshToken token) => dbContext.RefreshTokens.Add(token);

    public void Add(PasswordResetToken token) => dbContext.PasswordResetTokens.Add(token);

    public void Remove(UserRole userRole) => dbContext.UserRoles.Remove(userRole);

    public void Remove(Role role) => dbContext.Roles.Remove(role);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
