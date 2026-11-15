namespace ARTravel.Domain.Exceptions;

public sealed class ReservationNotFoundException(string message) : Exception(message);

public sealed class ReservationConflictException(string message) : Exception(message);
