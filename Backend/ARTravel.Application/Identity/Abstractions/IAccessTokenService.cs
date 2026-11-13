using ARTravel.Domain.Entities;
using ARTravel.Application.Identity.Contracts;

namespace ARTravel.Application.Identity.Abstractions;

public interface IAccessTokenService
{
    IssuedAccessToken Create(User user, IReadOnlyCollection<Role> roles, string sessionId);
}
