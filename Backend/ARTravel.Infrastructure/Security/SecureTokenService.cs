using System.Security.Cryptography;
using System.Text;
using ARTravel.Application.Identity.Abstractions;
using ARTravel.Application.Identity.Contracts;

namespace ARTravel.Infrastructure.Security;

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
