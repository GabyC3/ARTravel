using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Identity;

public interface IIdentityStore
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindUserByIdAsync(int userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListUsersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountUsersAsync(CancellationToken cancellationToken);

    Task<Role?> FindRoleByIdAsync(int roleId, CancellationToken cancellationToken);

    Task<Role?> FindRoleByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> ListRolesAsync(
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountRolesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetUserRolesAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<UserRole?> FindUserRoleAsync(
        int userId,
        int roleId,
        CancellationToken cancellationToken);

    Task<int> CountActiveUsersWithRoleAsync(
        int roleId,
        CancellationToken cancellationToken);

    Task<int> CountUsersWithRoleAsync(int roleId, CancellationToken cancellationToken);

    Task<bool> IsAccessTokenRevokedAsync(
        string tokenId,
        CancellationToken cancellationToken);

    Task<RefreshToken?> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<bool> RotateRefreshTokenAsync(
        string currentTokenHash,
        RefreshToken replacement,
        CancellationToken cancellationToken);

    Task RevokeSessionRefreshTokensAsync(
        string sessionId,
        CancellationToken cancellationToken);

    Task<bool> ConsumePasswordResetTokenAsync(
        string tokenHash,
        string passwordHash,
        CancellationToken cancellationToken);

    Task UpdatePasswordAndRevokeSessionsAsync(
        int userId,
        string passwordHash,
        CancellationToken cancellationToken);

    Task SetUserActiveAndInvalidateSessionsAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken);

    void Add(User user);

    void Add(Role role);

    void Add(UserRole userRole);

    void Add(RevokedAccessToken token);

    void Add(RefreshToken token);

    void Add(PasswordResetToken token);

    void Remove(UserRole userRole);

    void Remove(Role role);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
