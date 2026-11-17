using ARTravel.Domain.Exceptions;
using ARTravel.Application.Interfaces.Identity;
using ARTravel.Application.DTOs.Identity;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.UseCases.Identity;

public sealed class AuthService(
    IIdentityStore store,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokenService,
    ISecureTokenService secureTokenService,
    IPasswordResetNotifier passwordResetNotifier,
    IdentityTokenLifetimes tokenLifetimes) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        ValidateUserDetails(command.FirstName, command.LastName, command.Email);
        ValidatePassword(command.Password);
        var email = NormalizeEmail(command.Email);
        if (await store.FindUserByEmailAsync(email, cancellationToken) is not null)
        {
            throw new IdentityConflictException("An account with this email already exists.");
        }

        var defaultRole = await store.FindRoleByNormalizedNameAsync("USER", cancellationToken)
            ?? throw new IdentityConfigurationException(
                "The default User role is missing. Apply the database migrations.");

        var user = new User
        {
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(command.Password),
            PhoneNumber = NormalizeOptional(command.PhoneNumber),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        store.Add(user);
        store.Add(new UserRole { User = user, Role = defaultRole });
        await store.SaveChangesAsync(cancellationToken);

        return await CreateSessionAsync(user, [defaultRole], cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await store.FindUserByEmailAsync(
            NormalizeEmail(command.Email),
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new IdentityUnauthorizedException();
        }

        var roles = await store.GetUserRolesAsync(user.Id, cancellationToken);
        return await CreateSessionAsync(user, roles, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new IdentityUnauthorizedException();
        }

        var currentHash = secureTokenService.Hash(refreshToken);
        var current = await store.FindRefreshTokenAsync(currentHash, cancellationToken);
        if (current is null)
        {
            throw new IdentityUnauthorizedException();
        }

        if (current.RevokedAt is not null)
        {
            await store.RevokeSessionRefreshTokensAsync(current.SessionId, cancellationToken);
            throw new IdentityUnauthorizedException();
        }

        if (current.ExpiresAt <= DateTime.UtcNow || !current.User.IsActive)
        {
            await store.RevokeSessionRefreshTokensAsync(current.SessionId, cancellationToken);
            throw new IdentityUnauthorizedException();
        }

        var roles = current.User.UserRoles
            .Select(userRole => userRole.Role)
            .Where(role => role.IsActive)
            .ToArray();
        var issuedRefreshToken = secureTokenService.Create(tokenLifetimes.RefreshToken);
        var replacement = CreateRefreshToken(current.User, current.SessionId, issuedRefreshToken);
        var accessToken = accessTokenService.Create(current.User, roles, current.SessionId);

        if (!await store.RotateRefreshTokenAsync(
                currentHash,
                replacement,
                cancellationToken))
        {
            await store.RevokeSessionRefreshTokensAsync(current.SessionId, cancellationToken);
            throw new IdentityUnauthorizedException();
        }

        return new AuthResult(
            accessToken.Value,
            accessToken.ExpiresAt,
            issuedRefreshToken.Value,
            replacement.ExpiresAt,
            ToSummary(current.User, roles));
    }

    public async Task LogoutAsync(
        string tokenId,
        string? sessionId,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            throw new IdentityUnauthorizedException();
        }

        if (expiresAt > DateTime.UtcNow &&
            !await store.IsAccessTokenRevokedAsync(tokenId, cancellationToken))
        {
            store.Add(new RevokedAccessToken
            {
                TokenId = tokenId,
                ExpiresAt = expiresAt,
                RevokedAt = DateTime.UtcNow
            });
            await store.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await store.RevokeSessionRefreshTokensAsync(sessionId, cancellationToken);
        }
    }

    public async Task<UserSummary> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await store.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new IdentityUnauthorizedException();
        }

        return ToSummary(user, user.UserRoles
            .Select(userRole => userRole.Role)
            .Where(role => role.IsActive)
            .ToArray());
    }

    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken)
    {
        if (!passwordResetNotifier.IsConfigured)
        {
            throw new PasswordResetDeliveryUnavailableException();
        }

        var user = await store.FindUserByEmailAsync(NormalizeEmail(email), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return;
        }

        var resetToken = secureTokenService.Create(tokenLifetimes.PasswordReset);
        store.Add(new PasswordResetToken
        {
            TokenHash = resetToken.Hash,
            UserId = user.Id,
            User = user,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = resetToken.ExpiresAt
        });
        await store.SaveChangesAsync(cancellationToken);
        await passwordResetNotifier.SendAsync(
            user.Email,
            resetToken.Value,
            resetToken.ExpiresAt,
            cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ValidatePassword(command.NewPassword);
        if (string.IsNullOrWhiteSpace(command.Token) ||
            !await store.ConsumePasswordResetTokenAsync(
                secureTokenService.Hash(command.Token),
                passwordHasher.Hash(command.NewPassword),
                cancellationToken))
        {
            throw new IdentityUnauthorizedException();
        }
    }

    public async Task ChangePasswordAsync(
        int userId,
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        ValidatePassword(command.NewPassword);
        var user = await store.FindUserByIdAsync(userId, cancellationToken);
        if (user is null ||
            !user.IsActive ||
            !passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            throw new IdentityUnauthorizedException();
        }

        if (passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            throw new ArgumentException("The new password must differ from the current password.");
        }

        await store.UpdatePasswordAndRevokeSessionsAsync(
            userId,
            passwordHasher.Hash(command.NewPassword),
            cancellationToken);
    }

    private async Task<AuthResult> CreateSessionAsync(
        User user,
        IReadOnlyCollection<Role> roles,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var accessToken = accessTokenService.Create(user, roles, sessionId);
        var issuedRefreshToken = secureTokenService.Create(tokenLifetimes.RefreshToken);
        var refreshToken = CreateRefreshToken(user, sessionId, issuedRefreshToken);
        store.Add(refreshToken);
        await store.SaveChangesAsync(cancellationToken);
        return new AuthResult(
            accessToken.Value,
            accessToken.ExpiresAt,
            issuedRefreshToken.Value,
            refreshToken.ExpiresAt,
            ToSummary(user, roles));
    }

    private static RefreshToken CreateRefreshToken(
        User user,
        string sessionId,
        IssuedOpaqueToken token) =>
        new()
        {
            TokenHash = token.Hash,
            UserId = user.Id,
            User = user,
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = token.ExpiresAt
        };

    private static UserSummary ToSummary(User user, IReadOnlyCollection<Role> roles) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            roles.Where(role => role.IsActive).Select(role => role.Name).ToArray());

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static void ValidateUserDetails(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName) ||
            firstName.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(lastName) ||
            lastName.Trim().Length > 100 ||
            string.IsNullOrWhiteSpace(email) ||
            email.Trim().Length > 320 ||
            !email.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("First name, last name, and a valid email are required.");
        }
    }

    internal static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) ||
            password.Length < 8 ||
            !password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) ||
            !password.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new ArgumentException(
                "Password must be at least 8 characters and include uppercase, lowercase, a number, and a symbol.");
        }
    }
}
