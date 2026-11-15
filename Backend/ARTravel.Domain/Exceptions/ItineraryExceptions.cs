namespace ARTravel.Domain.Exceptions;

public sealed class ItineraryNotFoundException(string message) : Exception(message)
{
}

public sealed class ItineraryConflictException(string message) : Exception(message)
{
}
