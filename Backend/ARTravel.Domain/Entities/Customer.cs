namespace ARTravel.Domain.Entities;

public sealed class Customer
{
    public int Id { get; set; }

    public CustomerType CustomerType { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? BusinessName { get; set; }

    public required string DocumentType { get; set; }

    public required string DocumentNumber { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
