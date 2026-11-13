namespace ARTravel.Application.Identity.Contracts;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber);

public sealed record LoginCommand(string Email, string Password);

public sealed record UpdateUserCommand(string FirstName, string LastName, string? PhoneNumber);

public sealed record CreateRoleCommand(string Name, string? Description);

public sealed record UpdateRoleCommand(string Name, string? Description);

public sealed record UserSummary(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<string> Roles);

public sealed record RoleSummary(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);

public sealed record AuthResult(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserSummary User);

public sealed record PagedUsers(
    IReadOnlyList<UserSummary> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record IssuedAccessToken(string Value, string TokenId, DateTime ExpiresAt);

public sealed record IssuedOpaqueToken(string Value, string Hash, DateTime ExpiresAt);

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber);

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);

public sealed record ResetPasswordCommand(string Token, string NewPassword);

public sealed record IdentityTokenLifetimes(
    TimeSpan RefreshToken,
    TimeSpan PasswordReset);
