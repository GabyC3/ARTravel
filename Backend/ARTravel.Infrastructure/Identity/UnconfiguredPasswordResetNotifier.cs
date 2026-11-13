using ARTravel.Application.Identity.Abstractions;
using ARTravel.Application.Identity;

namespace ARTravel.Infrastructure.Identity;

public sealed class UnconfiguredPasswordResetNotifier : IPasswordResetNotifier
{
    public bool IsConfigured => false;

    public Task SendAsync(
        string email,
        string resetToken,
        DateTime expiresAt,
        CancellationToken cancellationToken) =>
        throw new PasswordResetDeliveryUnavailableException();
}
