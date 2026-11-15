namespace ARTravel.Domain.Entities;

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5,
    Completed = 6,
    Expired = 7
}
