namespace ARTravel.Domain.Exceptions;

public sealed class DepartureNotFoundException(string message) : Exception(message);

public sealed class DepartureConflictException(string message) : Exception(message);
