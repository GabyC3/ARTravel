using System.ComponentModel.DataAnnotations;
using ARTravel.Domain.Entities;

namespace ARTravel.API.Contracts;

public sealed class CreateCustomerRequest
{
    public CustomerType CustomerType { get; init; }

    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(200)]
    public string? BusinessName { get; init; }

    [Required, MaxLength(50)]
    public string DocumentType { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string DocumentNumber { get; init; } = string.Empty;

    [EmailAddress, MaxLength(320)]
    public string? Email { get; init; }

    [MaxLength(30)]
    public string? PhoneNumber { get; init; }

    [MaxLength(250)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateCustomerRequest
{
    public CustomerType CustomerType { get; init; }

    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [MaxLength(200)]
    public string? BusinessName { get; init; }

    [Required, MaxLength(50)]
    public string DocumentType { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string DocumentNumber { get; init; } = string.Empty;

    [EmailAddress, MaxLength(320)]
    public string? Email { get; init; }

    [MaxLength(30)]
    public string? PhoneNumber { get; init; }

    [MaxLength(250)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class SetContactStatusRequest
{
    [Required]
    public bool? IsActive { get; init; }
}

public sealed class CreatePassengerRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required, MaxLength(50)]
    public string DocumentType { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    [MaxLength(100)]
    public string? Nationality { get; init; }

    [EmailAddress, MaxLength(320)]
    public string? Email { get; init; }

    [MaxLength(30)]
    public string? PhoneNumber { get; init; }

    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(30)]
    public string? EmergencyContactPhone { get; init; }

    public DateOnly? PassportExpirationDate { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class UpdatePassengerRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required, MaxLength(50)]
    public string DocumentType { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    [MaxLength(100)]
    public string? Nationality { get; init; }

    [EmailAddress, MaxLength(320)]
    public string? Email { get; init; }

    [MaxLength(30)]
    public string? PhoneNumber { get; init; }

    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(30)]
    public string? EmergencyContactPhone { get; init; }

    public DateOnly? PassportExpirationDate { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}
