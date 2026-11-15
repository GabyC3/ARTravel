namespace ARTravel.Domain.Exceptions;

public sealed class ProductNotFoundException(string message) : Exception(message)
{
}

public sealed class ProductConflictException(string message) : Exception(message)
{
}
