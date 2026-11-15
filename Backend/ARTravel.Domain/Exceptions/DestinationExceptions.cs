namespace ARTravel.Domain.Exceptions;

public sealed class DestinationNotFoundException(string message) : Exception(message)
{
}
