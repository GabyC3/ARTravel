using ARTravel.Application.Interfaces.Identity;
using ARTravel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ARTravel.Infrastructure.Repositories.Identity;

public sealed class EfAccessTokenSessionValidator(AppDbContext dbContext)
    : IAccessTokenSessionValidator
{
    public async Task<IReadOnlyList<string>?> GetActiveRolesAsync(
        string tokenId,
        string sessionId,
        int userId,
        int tokenVersion,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var isRevoked = await dbContext.RevokedAccessTokens
            .AnyAsync(
                token => token.TokenId == tokenId && token.ExpiresAt > now,
                cancellationToken);
        if (isRevoked)
        {
            return null;
        }

        var activeSessionExists = await dbContext.RefreshTokens
            .AnyAsync(
                token =>
                    token.SessionId == sessionId &&
                    token.RevokedAt == null &&
                    token.ExpiresAt > now,
                cancellationToken);
        if (!activeSessionExists)
        {
            return null;
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(entity => entity.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(entity =>
                entity.Id == userId &&
                entity.IsActive &&
                entity.TokenVersion == tokenVersion,
                cancellationToken);

        return user?.UserRoles
            .Where(userRole => userRole.Role.IsActive)
            .Select(userRole => userRole.Role.Name)
            .ToArray();
    }
}
