using ARTravel.Application.DTOs.Identity;

namespace ARTravel.Application.Interfaces.Identity;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken);

    Task<AuthResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken);

    Task<AuthResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string tokenId,
        string? sessionId,
        DateTime expiresAt,
        CancellationToken cancellationToken);

    Task<UserSummary> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken);

    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);

    Task ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken);

    Task ChangePasswordAsync(
        int userId,
        ChangePasswordCommand command,
        CancellationToken cancellationToken);
}
