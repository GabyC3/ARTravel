using ARTravel.Domain.Entities;

namespace ARTravel.Application.DTOs.Contacts;

public sealed record CreateCustomerCommand(
    CustomerType CustomerType,
    string? FirstName,
    string? LastName,
    string? BusinessName,
    string DocumentType,
    string DocumentNumber,
    string? Email,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Country,
    string? Notes,
    bool IsActive);

public sealed record UpdateCustomerCommand(
    CustomerType CustomerType,
    string? FirstName,
    string? LastName,
    string? BusinessName,
    string DocumentType,
    string DocumentNumber,
    string? Email,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Country,
    string? Notes);

public sealed record CreatePassengerCommand(
    string FirstName,
    string LastName,
    string DocumentType,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? Nationality,
    string? Email,
    string? PhoneNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    DateOnly? PassportExpirationDate,
    string? Notes,
    bool IsActive);

public sealed record UpdatePassengerCommand(
    string FirstName,
    string LastName,
    string DocumentType,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? Nationality,
    string? Email,
    string? PhoneNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    DateOnly? PassportExpirationDate,
    string? Notes);

public sealed record CustomerResponse(
    int Id,
    CustomerType CustomerType,
    string? FirstName,
    string? LastName,
    string? BusinessName,
    string DocumentType,
    string DocumentNumber,
    string? Email,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Country,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record PassengerResponse(
    int Id,
    string FirstName,
    string LastName,
    string DocumentType,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? Nationality,
    string? Email,
    string? PhoneNumber,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    DateOnly? PassportExpirationDate,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record PagedCustomers(
    IReadOnlyList<CustomerResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PagedPassengers(
    IReadOnlyList<PassengerResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record CustomerSummary(
    CustomerResponse Customer,
    int ReservationCount,
    decimal TotalReservationAmount);

public sealed record TripResponse(
    int Id,
    int ReservationId,
    int? PassengerId,
    int? DepartureId,
    int? ReservationPassengerId,
    string Origin,
    string Destination,
    DateTime DepartureAt,
    DateTime? ArrivalAt,
    string Status);

public sealed record PagedTrips(
    IReadOnlyList<TripResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
