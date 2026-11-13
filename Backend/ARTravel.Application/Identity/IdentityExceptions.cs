namespace ARTravel.Application.Identity;

public sealed class IdentityNotFoundException(string message) : Exception(message)
{
}

public sealed class IdentityConflictException(string message) : Exception(message)
{
}

public sealed class IdentityConfigurationException(string message) : Exception(message)
{
}

public sealed class PasswordResetDeliveryUnavailableException()
    : Exception("Password reset email delivery is not configured.")
{
}

public sealed class IdentityUnauthorizedException()
    : Exception("The email or password is invalid.")
{
}
