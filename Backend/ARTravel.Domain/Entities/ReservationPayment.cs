namespace ARTravel.Domain.Entities;

public sealed class ReservationPayment
{
    public int Id { get; set; }

    public int ReservationId { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string PaymentMethod { get; set; }

    public ReservationPaymentStatus Status { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
