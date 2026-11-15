using System.Security.Cryptography;
using System.Text;
using ARTravel.Application.Interfaces.Identity;
using ARTravel.Application.DTOs.Identity;

namespace ARTravel.Infrastructure.Services.Security;

public sealed class SecureTokenService : ISecureTokenService
{
    public IssuedOpaqueToken Create(TimeSpan lifetime)
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return new IssuedOpaqueToken(
            value,
            Hash(value),
            DateTime.UtcNow.Add(lifetime));
    }

    public string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
