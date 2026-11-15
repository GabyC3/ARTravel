using System.Text;
using Microsoft.Extensions.Configuration;

namespace ARTravel.Infrastructure.Services.Security;

public sealed class JwtSettings(
    string issuer,
    string audience,
    string signingKey,
    int accessTokenMinutes,
    int refreshTokenDays,
    int passwordResetMinutes)
{
    public string Issuer { get; } = issuer;

    public string Audience { get; } = audience;

    public string SigningKey { get; } = signingKey;

    public int AccessTokenMinutes { get; } = accessTokenMinutes;

    public int RefreshTokenDays { get; } = refreshTokenDays;

    public int PasswordResetMinutes { get; } = passwordResetMinutes;

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var signingKey = configuration["Jwt:SigningKey"];
        var accessTokenMinutesValue = configuration["Jwt:AccessTokenMinutes"];
        var accessTokenMinutes = string.IsNullOrWhiteSpace(accessTokenMinutesValue)
            ? 60
            : int.TryParse(accessTokenMinutesValue, out var configuredMinutes)
                ? configuredMinutes
                : 0;
        var refreshTokenDays = ParseSetting(configuration["Jwt:RefreshTokenDays"], 7);
        var passwordResetMinutes = ParseSetting(configuration["Jwt:PasswordResetMinutes"], 30);

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(signingKey) ||
            Encoding.UTF8.GetByteCount(signingKey) < 32 ||
            accessTokenMinutes is < 1 or > 1440 ||
            refreshTokenDays is < 1 or > 90 ||
            passwordResetMinutes is < 5 or > 60)
        {
            throw new InvalidOperationException(
                "Configure Jwt:Issuer, Jwt:Audience, a Jwt:SigningKey of at least 32 bytes, " +
                "Jwt:AccessTokenMinutes between 1 and 1440, Jwt:RefreshTokenDays between 1 and 90, " +
                "and Jwt:PasswordResetMinutes between 5 and 60.");
        }

        return new JwtSettings(
            issuer,
            audience,
            signingKey,
            accessTokenMinutes,
            refreshTokenDays,
            passwordResetMinutes);
    }

    private static int ParseSetting(string? value, int defaultValue) =>
        string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : int.TryParse(value, out var parsed) ? parsed : 0;
}
