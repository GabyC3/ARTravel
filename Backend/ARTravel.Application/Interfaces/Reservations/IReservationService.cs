using ARTravel.Application.DTOs.Reservations;
using ARTravel.Domain.Entities;

namespace ARTravel.Application.Interfaces.Reservations;

public interface IReservationService
{
    Task<PagedReservations> ListAsync(
        int page,
        int pageSize,
        string? search,
        int? customerId,
        int? departureId,
        ReservationStatus? status,
        DateTime? reservationDateFrom,
        DateTime? reservationDateTo,
        CancellationToken cancellationToken);

    Task<ReservationDetail> GetAsync(int id, CancellationToken cancellationToken);

    Task<ReservationDetail> CreateAsync(
        CreateReservationCommand command,
        CancellationToken cancellationToken);

    Task<ReservationDetail> UpdateAsync(
        int id,
        UpdateReservationCommand command,
        CancellationToken cancellationToken);

    Task<ReservationDetail> ConfirmAsync(int id, CancellationToken cancellationToken);

    Task<ReservationDetail> CancelAsync(
        int id,
        CancelReservationCommand command,
        CancellationToken cancellationToken);

    Task<ReservationDetail> SetStatusAsync(
        int id,
        SetReservationStatusCommand command,
        CancellationToken cancellationToken);

    Task<PagedReservations> ListByCustomerAsync(
        int customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedReservations> ListByDepartureAsync(
        int departureId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReservationPaymentResponse>> GetPaymentsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ReservationPassengerResponse> Items, int Page, int PageSize, int TotalCount)>
        ListPassengersAsync(
            int reservationId,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

    Task<ReservationPassengerResponse> GetPassengerAsync(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken);

    Task<ReservationPassengerResponse> AddPassengerAsync(
        int reservationId,
        CreateReservationPassengerCommand command,
        CancellationToken cancellationToken);

    Task<ReservationPassengerResponse> UpdatePassengerAsync(
        int reservationId,
        int passengerId,
        UpdateReservationPassengerCommand command,
        CancellationToken cancellationToken);

    Task RemovePassengerAsync(
        int reservationId,
        int passengerId,
        CancellationToken cancellationToken);

    Task<PagedReservationPassengers> ListDeparturePassengersAsync(
            int departureId,
            int page,
            int pageSize,
            CancellationToken cancellationToken);
}
