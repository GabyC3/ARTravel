using System.ComponentModel.DataAnnotations;

namespace ARTravel.API.Contracts;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{12,}$",
        ErrorMessage = "Password must include uppercase, lowercase, a number, and a symbol.")]
    public string Password { get; init; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; init; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Required, StringLength(256, MinimumLength = 32)]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required, StringLength(256, MinimumLength = 32)]
    public string Token { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{12,}$",
        ErrorMessage = "Password must include uppercase, lowercase, a number, and a symbol.")]
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 12)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{12,}$",
        ErrorMessage = "Password must include uppercase, lowercase, a number, and a symbol.")]
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class UpdateUserRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; init; }
}

public sealed class SetUserActiveRequest
{
    [Required]
    public bool? IsActive { get; init; }
}

public sealed class CreateRoleRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; init; }
}

public sealed class SetRoleActiveRequest
{
    [Required]
    public bool? IsActive { get; init; }
}
