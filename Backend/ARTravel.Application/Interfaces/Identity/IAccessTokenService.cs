using ARTravel.Domain.Entities;
using ARTravel.Application.DTOs.Identity;

namespace ARTravel.Application.Interfaces.Identity;

public interface IAccessTokenService
{
    IssuedAccessToken Create(User user, IReadOnlyCollection<Role> roles, string sessionId);
}
