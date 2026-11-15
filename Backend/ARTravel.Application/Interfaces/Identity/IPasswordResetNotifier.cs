namespace ARTravel.Application.Interfaces.Identity;

public interface IPasswordResetNotifier
{
    bool IsConfigured { get; }

    Task SendAsync(
        string email,
        string resetToken,
        DateTime expiresAt,
        CancellationToken cancellationToken);
}
