using ARTravel.Application.Identity.Contracts;

namespace ARTravel.Application.Identity.Abstractions;

public interface ISecureTokenService
{
    IssuedOpaqueToken Create(TimeSpan lifetime);

    string Hash(string value);
}
