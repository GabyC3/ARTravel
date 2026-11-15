using ARTravel.Application.Interfaces.Identity;
using ARTravel.Domain.Exceptions;

namespace ARTravel.Infrastructure.Services.Identity;

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
