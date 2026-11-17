namespace ARTravel.Domain.Exceptions;

public sealed class DestinationNotFoundException(string message) : Exception(message)
{
}

public sealed class DestinationConflictException(string message) : Exception(message)
{
}
