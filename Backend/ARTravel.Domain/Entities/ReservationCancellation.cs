namespace ARTravel.Domain.Entities;

public sealed class ReservationCancellation
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public required string Reason { get; set; }

    public DateTime CancelledAt { get; set; }

    public int CancelledByUserId { get; set; }

    public decimal RefundAmount { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
