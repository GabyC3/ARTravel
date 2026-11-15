using ARTravel.Domain.Entities;

namespace ARTravel.Application.DTOs.Reservations;

public sealed record CreateReservationCommand(
    int CustomerId,
    int DepartureId,
    string? Notes,
    int CreatedByUserId);

public sealed record UpdateReservationCommand(string? Notes, decimal Discount);

public sealed record SetReservationStatusCommand(ReservationStatus Status);

public sealed record CancelReservationCommand(
    string Reason,
    decimal RefundAmount,
    int CancelledByUserId);

public sealed record CreateReservationPassengerCommand(
    int? PassengerId,
    string FirstName,
    string LastName,
    ReservationDocumentType DocumentType,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? Gender,
    string? PhoneNumber,
    string? Email,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? SpecialRequirements,
    string? Notes,
    DeparturePriceType PriceType);

public sealed record UpdateReservationPassengerCommand(
    int? PassengerId,
    string FirstName,
    string LastName,
    ReservationDocumentType DocumentType,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? Gender,
    string? PhoneNumber,
    string? Email,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? SpecialRequirements,
    string? Notes,
    DeparturePriceType PriceType);

public sealed record ReservationListItem(
    int Id,
    string ReservationNumber,
    int CustomerId,
    int DepartureId,
    DateTime ReservationDate,
    int NumberOfPassengers,
    decimal Subtotal,
    decimal Discount,
    decimal TotalAmount,
    string Currency,
    ReservationStatus Status,
    string? Notes,
    int CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ReservationPassengerResponse(
    int Id,
    int ReservationId,
    int? PassengerId,
    string FirstName,
    string LastName,
    ReservationDocumentType DocumentType,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? Gender,
    string? PhoneNumber,
    string? Email,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? SpecialRequirements,
    string? Notes,
    DeparturePriceType PriceType,
    decimal UnitPrice,
    string Currency,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ReservationCancellationResponse(
    int Id,
    int ReservationId,
    string Reason,
    DateTime CancelledAt,
    int CancelledByUserId,
    decimal RefundAmount);

public sealed record ReservationDetail(
    ReservationListItem Reservation,
    IReadOnlyList<ReservationPassengerResponse> Passengers,
    ReservationCancellationResponse? Cancellation);

public sealed record PagedReservations(
    IReadOnlyList<ReservationListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PagedReservationPassengers(
    IReadOnlyList<ReservationPassengerListItem> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record ReservationPaymentResponse(
    int Id,
    int ReservationId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    ReservationPaymentStatus Status,
    string? TransactionReference,
    DateTime? PaidAt,
    string? Notes,
    DateTime CreatedAt);

public sealed record ReservationPassengerListItem(
    int ReservationId,
    string ReservationNumber,
    int ReservationPassengerId,
    int? PassengerId,
    string FirstName,
    string LastName,
    ReservationDocumentType DocumentType,
    string DocumentNumber,
    DateOnly? BirthDate,
    string? Gender,
    string? PhoneNumber,
    string? Email,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? SpecialRequirements,
    string? Notes);
