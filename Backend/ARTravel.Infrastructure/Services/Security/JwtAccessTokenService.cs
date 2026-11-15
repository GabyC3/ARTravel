using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ARTravel.Application.Interfaces.Identity;
using ARTravel.Application.DTOs.Identity;
using ARTravel.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace ARTravel.Infrastructure.Services.Security;

public sealed class JwtAccessTokenService(JwtSettings settings) : IAccessTokenService
{
    public IssuedAccessToken Create(User user, IReadOnlyCollection<Role> roles, string sessionId)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(settings.AccessTokenMinutes);
        var tokenId = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("sid", sessionId),
            new("ver", user.TokenVersion.ToString())
        };

        claims.AddRange(
            roles.Where(role => role.IsActive)
                .Select(role => new Claim(ClaimTypes.Role, role.Name)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            now,
            expiresAt,
            credentials);

        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            tokenId,
            expiresAt);
    }
}
