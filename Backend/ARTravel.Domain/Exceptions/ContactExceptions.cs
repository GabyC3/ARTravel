namespace ARTravel.Domain.Exceptions;

public sealed class ContactNotFoundException(string message) : Exception(message)
{
}
