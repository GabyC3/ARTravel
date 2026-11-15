using ARTravel.Application.DTOs.Identity;

namespace ARTravel.Application.Interfaces.Identity;

public interface ISecureTokenService
{
    IssuedOpaqueToken Create(TimeSpan lifetime);

    string Hash(string value);
}
